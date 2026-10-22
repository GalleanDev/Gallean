using System.Buffers;
using System.Collections.ObjectModel;
using ImageMagick;

namespace GameContent;

public sealed class ImagesExtractor
{
    private sealed record MqImageFrame(
        string Name,
        int Width,
        int Height,
        IReadOnlyList<MqImagePart> Parts);

    private sealed record MqImageBlock(
        MqImageMetadata Metadata,
        IReadOnlyList<MqImageFrame> Frames);

    private sealed record MqImageSet(
        IReadOnlyDictionary<string, MqImage> Images,
        IReadOnlyDictionary<int, IReadOnlyList<MqImage>> ByFileId);

    private sealed record MqAnimationSet(
        IReadOnlyDictionary<string, IReadOnlyList<MqImage>> Animations,
        IReadOnlySet<string> FrameNames);

    private const int AnimationMqIndexId = -1;

    private const int FileTableRecordId = 2;
    private const int AuraOpacityAlgorithm = 300;
    private const int GlowOpacityAlgorithm = 400;
    private const int PaletteLength = 1024;

    private static readonly byte[] PngEndChunk = [0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<MqImage>> NoAnimations = ReadOnlyDictionary<string, IReadOnlyList<MqImage>>.Empty;

    private readonly IReadOnlyDictionary<string, IReadOnlyList<MqImage>> animations;
    private readonly MqdbResourceArchive archive;
    private readonly MqImageSet? images;

    private ImagesExtractor(
        MqdbResourceArchive archive,
        MqImageSet? images,
        IReadOnlyDictionary<string, IReadOnlyList<MqImage>> animations)
    {
        this.archive = archive;
        this.images = images;
        this.animations = animations;
    }

    public static ImagesExtractor Load(string path)
    {
        MqdbResourceArchive archive = MqdbResourceArchive.Load(path, FileTableRecordId, MqdbFileTableLayout.NameId);

        using FileStream stream = archive.OpenArchive();

        IReadOnlyList<MqIndex>? indexes = ReadIndexes(archive, stream);
        if (indexes is null)
        {
            return new ImagesExtractor(archive, images: null, NoAnimations);
        }

        IReadOnlyDictionary<string, MqImage>? allImages = ReadImages(archive, stream, indexes);
        if (allImages is null)
        {
            return new ImagesExtractor(archive, images: null, NoAnimations);
        }

        MqAnimationSet animations = ReadAnimations(archive, stream, indexes, allImages);

        IReadOnlyDictionary<string, MqImage> regularImages = allImages;
        if (animations.FrameNames.Count > 0)
        {
            Dictionary<string, MqImage> imagesWithoutFrames = new(StringComparer.OrdinalIgnoreCase);

            foreach (MqImage image in allImages.Values)
            {
                if (!animations.FrameNames.Contains(image.Name))
                {
                    imagesWithoutFrames.Add(image.Name, image);
                }
            }

            regularImages = imagesWithoutFrames;
        }

        Dictionary<int, List<MqImage>> imagesByFileId = [];

        foreach (MqImage image in allImages.Values)
        {
            if (!imagesByFileId.TryGetValue(image.FileId, out List<MqImage>? fileImages))
            {
                fileImages = [];
                imagesByFileId.Add(image.FileId, fileImages);
            }

            fileImages.Add(image);
        }

        Dictionary<int, IReadOnlyList<MqImage>> byFileId = new(imagesByFileId.Count);

        foreach (KeyValuePair<int, List<MqImage>> pair in imagesByFileId)
        {
            byFileId.Add(pair.Key, pair.Value);
        }

        return new ImagesExtractor(archive, new MqImageSet(regularImages, byFileId), animations.Animations);
    }

    public bool IsAnimationExists(string name)
    {
        return animations.ContainsKey(name);
    }

    public bool IsImageExists(string name)
    {
        return images?.Images.ContainsKey(name) == true;
    }

    public IReadOnlyCollection<RawBitmap>? TryGetAnimationFrames(string name)
    {
        if (animations.TryGetValue(name, out IReadOnlyList<MqImage>? frames))
        {
            return BuildAnimationFrames(frames);
        }

        return null;
    }

    public IDictionary<string, RawBitmap> GetImageParts(string name)
    {
        MqdbResourceFile? file;

        if (images is not null && images.Images.TryGetValue(name, out MqImage? logicalImage))
        {
            file = archive.GetFile(logicalImage.FileId);
        }
        else
        {
            file = archive.TryGetFile(name);
        }

        if (file is null)
        {
            throw new ResourceNotFoundException($"Cannot find image with name '{name}'.");
        }

        if (images is null)
        {
            throw new ResourceException("Resource does not support image parts.");
        }

        if (!images.ByFileId.TryGetValue(file.Id, out IReadOnlyList<MqImage>? fileImages))
        {
            throw new ResourceException($"Image '{name}' does not consist of parts.");
        }

        MqImageMetadata metadata = fileImages[0].Metadata;
        RawBitmap baseImage = PreparePalettizedImage(file, metadata);

        Dictionary<string, RawBitmap> parts = new(fileImages.Count);

        foreach (MqImage image in fileImages)
        {
            parts.Add(image.Name, BuildImage(baseImage, image));
        }

        return parts;
    }

    public RawBitmap GetImage(string name)
    {
        if (IsAnimationExists(name))
        {
            throw new ResourceException($"'{name}' is an animation and cannot be loaded as an image.");
        }

        if (images is not null && images.Images.TryGetValue(name, out MqImage? image))
        {
            RawBitmap baseImage = PreparePalettizedImage(archive.GetFile(image.FileId), image.Metadata);

            return BuildImage(baseImage, image);
        }

        MqdbResourceFile? imageFile = archive.TryGetFile(name);

        if (imageFile is null)
        {
            throw new ResourceNotFoundException($"Cannot find image with name '{name}'.");
        }

        return PrepareBrushKeyedImage(imageFile);
    }

    public IReadOnlyList<string> GetAllFilesNames()
    {
        return archive.FileNames;
    }

    public IReadOnlyList<string> GetAllAnimationNames()
    {
        return [.. animations.Keys];
    }

    public IReadOnlyList<string> GetAllImageNames()
    {
        if (images is null)
        {
            return [];
        }

        return [.. images.Images.Keys];
    }

    private static IReadOnlyList<MqIndex>? ReadIndexes(MqdbResourceArchive archive, Stream stream)
    {
        MqdbResourceFile? indexFile = archive.TryGetFile("-INDEX.OPT");
        if (indexFile is null)
        {
            return null;
        }

        stream.Seek(indexFile.Offset, SeekOrigin.Begin);

        int framesCount = stream.ReadInt32LittleEndian();
        List<MqIndex> indexes = new(framesCount);

        for (int frameIndex = 0; frameIndex < framesCount; ++frameIndex)
        {
            int id = stream.ReadInt32LittleEndian();
            string name = stream.ReadAsciiString();
            stream.Skip(8);

            indexes.Add(new MqIndex(id, name));
        }

        return indexes;
    }

    private static IReadOnlyDictionary<string, MqImage>? ReadImages(MqdbResourceArchive archive, Stream stream, IReadOnlyList<MqIndex> indexes)
    {
        MqdbResourceFile? imagesFile = archive.TryGetFile("-IMAGES.OPT");
        if (imagesFile is null)
        {
            return null;
        }

        Dictionary<string, List<int>> fileIdsByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (MqIndex index in indexes)
        {
            if (archive.TryGetFile(index.Id) is not null)
            {
                if (!fileIdsByName.TryGetValue(index.Name, out List<int>? fileIds))
                {
                    fileIds = [];
                    fileIdsByName.Add(index.Name, fileIds);
                }

                fileIds.Add(index.Id);
            }
        }

        Dictionary<string, MqImage> images = new(StringComparer.OrdinalIgnoreCase);
        foreach (MqImageBlock block in ReadImageBlocks(stream, imagesFile))
        {
            int fileId = ResolveBlockFileId(archive, block, fileIdsByName);

            foreach (MqImageFrame frame in block.Frames)
            {
                MqImage image = new(frame.Name, fileId, frame.Width, frame.Height, block.Metadata, frame.Parts);

                images.TryAdd(image.Name, image);
            }
        }

        return images;
    }

    private static IReadOnlyList<MqImageBlock> ReadImageBlocks(Stream stream, MqdbResourceFile imagesFile)
    {
        stream.Seek(imagesFile.Offset, SeekOrigin.Begin);

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(PaletteLength);
        Span<byte> paletteBuffer = memoryOwner.Memory[..PaletteLength].Span;

        List<MqImageBlock> blocks = [];
        long endOfFile = imagesFile.Offset + imagesFile.Size - 1;
        while (stream.Position < endOfFile)
        {
            int transparentColorIndex = stream.ReadByte();
            short opacityAlgorithm = stream.ReadInt16LittleEndian();
            int sizeX = stream.ReadInt32LittleEndian();
            int sizeY = stream.ReadInt32LittleEndian();

            stream.ReadExactly(paletteBuffer);
            List<Color> palette = new(paletteBuffer.Length / 4);
            for (int i = 0; i < paletteBuffer.Length; i += 4)
            {
                palette.Add(new Color(paletteBuffer[i + 3], paletteBuffer[i], paletteBuffer[i + 1], paletteBuffer[i + 2]));
            }

            MqImageMetadata metadata = new(transparentColorIndex, opacityAlgorithm, sizeX, sizeY, palette);

            int framesCount = stream.ReadInt32LittleEndian();
            List<MqImageFrame> frames = new(framesCount);
            for (int frameIndex = 0; frameIndex < framesCount; ++frameIndex)
            {
                string frameName = stream.ReadAsciiString();
                int partsCount = stream.ReadInt32LittleEndian();
                int width = stream.ReadInt32LittleEndian();
                int height = stream.ReadInt32LittleEndian();

                List<MqImagePart> parts = new(partsCount);
                for (int partIndex = 0; partIndex < partsCount; ++partIndex)
                {
                    int sourceX = stream.ReadInt32LittleEndian();
                    int sourceY = stream.ReadInt32LittleEndian();
                    int destX = stream.ReadInt32LittleEndian();
                    int destY = stream.ReadInt32LittleEndian();
                    int partWidth = stream.ReadInt32LittleEndian();
                    int partHeight = stream.ReadInt32LittleEndian();

                    parts.Add(new MqImagePart(sourceX, sourceY, destX, destY, partWidth, partHeight));
                }

                frames.Add(new MqImageFrame(frameName, width, height, parts));
            }

            blocks.Add(new MqImageBlock(metadata, frames));
        }

        return blocks;
    }

    private static List<int> GetFileIds(IReadOnlyDictionary<string, List<int>> fileIdsByName, string name)
    {
        if (fileIdsByName.TryGetValue(name, out List<int>? fileIds))
        {
            return fileIds;
        }
        else
        {
            return [];
        }
    }

    private static int ResolveBlockFileId(MqdbResourceArchive archive, MqImageBlock block, IReadOnlyDictionary<string, List<int>> fileIdsByName)
    {
        List<int> candidates = [.. GetFileIds(fileIdsByName, block.Frames[0].Name)];

        foreach (MqImageFrame frame in block.Frames)
        {
            List<int> frameFileIds = GetFileIds(fileIdsByName, frame.Name);

            candidates.RemoveAll(candidate => !frameFileIds.Contains(candidate));
        }

        if (candidates.Count == 0)
        {
            throw new ResourceNotFoundException($"Cannot find base image for '{block.Frames[0].Name}'.");
        }
        else if (candidates.Count == 1)
        {
            return candidates[0];
        }
        else
        {
            foreach (int candidate in candidates)
            {
                IMagickImageInfo imageInfo = new MagickImageInfo(ReadImageFile(archive, archive.GetFile(candidate)));

                if (imageInfo.Width == block.Metadata.SizeX && imageInfo.Height == block.Metadata.SizeY)
                {
                    return candidate;
                }
            }

            throw new ResourceNotFoundException($"Cannot find base image for '{block.Frames[0].Name}'.");
        }
    }

    private static MqAnimationSet ReadAnimations(
        MqdbResourceArchive archive,
        Stream stream,
        IReadOnlyList<MqIndex> indexes,
        IReadOnlyDictionary<string, MqImage> images)
    {
        MqdbResourceFile? animationsFile = archive.TryGetFile("-ANIMS.OPT");
        if (animationsFile is null)
        {
            return new MqAnimationSet(NoAnimations, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        HashSet<string> frameNames = new(StringComparer.OrdinalIgnoreCase);
        List<IReadOnlyList<MqImage>> animationFrames = [];
        stream.Seek(animationsFile.Offset, SeekOrigin.Begin);

        long endOfFile = animationsFile.Offset + animationsFile.Size - 1;

        while (stream.Position < endOfFile)
        {
            int framesCount = stream.ReadInt32LittleEndian();
            List<MqImage> frames = new(framesCount);
            for (int frameIndex = 0; frameIndex < framesCount; ++frameIndex)
            {
                string frameName = stream.ReadAsciiString();
                frames.Add(images[frameName]);
                frameNames.Add(frameName);
            }

            animationFrames.Add(frames);
        }

        List<string> animationNames = [];

        foreach (MqIndex index in indexes)
        {
            if (index.Id == AnimationMqIndexId)
            {
                animationNames.Add(index.Name);
            }
        }

        if (animationNames.Count != animationFrames.Count)
        {
            throw new ResourceException("Animation lengths do not match.");
        }

        Dictionary<string, IReadOnlyList<MqImage>> animations = new(StringComparer.OrdinalIgnoreCase);
        for (int animationIndex = 0; animationIndex < animationFrames.Count; animationIndex++)
        {
            animations.Add(animationNames[animationIndex], animationFrames[animationIndex]);
        }

        return new MqAnimationSet(animations, frameNames);
    }

    private IReadOnlyCollection<RawBitmap> BuildAnimationFrames(IReadOnlyList<MqImage> frames)
    {
        List<RawBitmap> result = new(frames.Count);

        Dictionary<int, RawBitmap> baseImages = [];
        Rectangle animationBounds = GetAnimationBounds(frames);

        foreach (MqImage frame in frames)
        {
            int fileId = frame.FileId;

            if (!baseImages.TryGetValue(fileId, out RawBitmap? value))
            {
                value = PreparePalettizedImage(archive.GetFile(fileId), frame.Metadata);
                baseImages.Add(fileId, value);
            }

            result.Add(BuildImage(value, frame, animationBounds));
        }

        return result;
    }

    private static RawBitmap BuildImage(RawBitmap baseImage, MqImage image)
    {
        return BuildImage(baseImage, image, GetImageBounds(image));
    }

    private static Rectangle GetAnimationBounds(IReadOnlyList<MqImage> images)
    {
        if (images.Count > 0)
        {
            Rectangle bounds = GetImageBounds(images[0]);

            for (int index = 1; index < images.Count; index++)
            {
                bounds = Rectangle.Union(bounds, GetImageBounds(images[index]));
            }

            return bounds;
        }
        else
        {
            return Rectangle.Empty;
        }
    }

    private static Rectangle GetImageBounds(MqImage image)
    {
        IReadOnlyList<MqImagePart> parts = image.ImageParts;

        if (parts.Count > 0)
        {
            Rectangle bounds = new(parts[0].SourceX, parts[0].SourceY, parts[0].Width, parts[0].Height);

            for (int index = 1; index < parts.Count; index++)
            {
                MqImagePart imagePiece = parts[index];

                bounds = Rectangle.Union(bounds, new Rectangle(imagePiece.SourceX, imagePiece.SourceY, imagePiece.Width, imagePiece.Height));
            }

            return bounds;
        }
        else
        {
            return Rectangle.Empty;
        }
    }

    private static RawBitmap BuildImage(RawBitmap baseImage, MqImage image, Rectangle bounds)
    {
        int imageHeight = bounds.Height;
        int imageWidth = bounds.Width;
        byte[] imageData = new byte[imageHeight * imageWidth * 4];

        foreach (MqImagePart framePart in image.ImageParts)
        {
            if (framePart.DestX + framePart.Width > baseImage.OriginalWidth ||
                framePart.DestY + framePart.Height > baseImage.OriginalHeight)
            {
                throw new ResourceException($"Part of image '{image.Name}' does not fit its base image.");
            }

            int partWidth = framePart.Width << 2;
            for (int row = 0; row < framePart.Height; ++row)
            {
                int sourcePosition = ((framePart.DestY + row) * baseImage.OriginalWidth + framePart.DestX) << 2;
                int destinationPosition = ((framePart.SourceY + row - bounds.Y) * imageWidth + framePart.SourceX - bounds.X) << 2;

                Buffer.BlockCopy(baseImage.Data, sourcePosition, imageData, destinationPosition, partWidth);
            }
        }

        return new RawBitmap
        {
            OriginalWidth = image.Width,
            OriginalHeight = image.Height,
            Bounds = bounds,
            Additive = baseImage.Additive,
            Data = imageData
        };
    }

    private static byte[] ReadImageFile(MqdbResourceArchive archive, MqdbResourceFile file)
    {
        byte[] content = archive.ReadFile(file);

        if (!content.AsSpan().EndsWith(PngEndChunk))
        {
            return [.. content, .. PngEndChunk];
        }
        else
        {
            return content;
        }
    }

    private RawBitmap PreparePalettizedImage(MqdbResourceFile file, MqImageMetadata metadata)
    {
        IndexedPng image = IndexedPng.Read(ReadImageFile(archive, file));
        byte[] paletteOpacity = GetPaletteOpacity(metadata);
        byte[] pixels = new byte[image.Width * image.Height * 4];

        for (int i = 0; i < image.Indexes.Length; i++)
        {
            byte paletteIndex = image.Indexes[i];
            byte opacity = paletteOpacity[paletteIndex];

            if (opacity != 0)
            {
                int source = paletteIndex * 3;
                int target = i * 4;

                pixels[target] = image.Palette[source];
                pixels[target + 1] = image.Palette[source + 1];
                pixels[target + 2] = image.Palette[source + 2];
                pixels[target + 3] = opacity;
            }
        }

        return new RawBitmap
        {
            OriginalWidth = image.Width,
            OriginalHeight = image.Height,
            Bounds = new Rectangle(0, 0, image.Width, image.Height),
            Additive = IsAdditiveAlgorithm(metadata.OpacityAlgorithm),
            Data = pixels
        };
    }

    private static bool IsAdditiveAlgorithm(int opacityAlgorithm)
    {
        return opacityAlgorithm switch
        {
            AuraOpacityAlgorithm => true,
            GlowOpacityAlgorithm => true,
            _ => false
        };
    }

    private RawBitmap PrepareBrushKeyedImage(MqdbResourceFile file)
    {
        using MagickImage image = new(ReadImageFile(archive, file));
        image.Alpha(AlphaAction.Set);

        using IUnsafePixelCollection<byte> collection = image.GetPixelsUnsafe();
        byte[] pixels = collection.ToByteArray(PixelMapping.RGBA) ?? [];

        int width = (int)image.Width;
        int height = (int)image.Height;

        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (IsBrushColor(pixels[i], pixels[i + 1], pixels[i + 2]))
            {
                pixels[i] = 0;
                pixels[i + 1] = 0;
                pixels[i + 2] = 0;
                pixels[i + 3] = 0;
            }
            else
            {
                pixels[i + 3] = byte.MaxValue;
            }
        }

        return new RawBitmap
        {
            OriginalWidth = width,
            OriginalHeight = height,
            Bounds = new Rectangle(0, 0, width, height),
            Data = pixels
        };
    }

    private static bool IsBrushColor(byte red, byte green, byte blue)
    {
        const int mask = 0xf8;

        return (red & mask) == mask && (green & mask) == 0 && (blue & mask) == mask;
    }

    private static byte[] GetPaletteOpacity(MqImageMetadata metadata)
    {
        IReadOnlyList<Color> palette = metadata.Palette;

        byte[] opacity = new byte[palette.Count];

        if (metadata.OpacityAlgorithm <= byte.MaxValue)
        {
            for (int index = 0; index < palette.Count; index++)
            {
                Color color = palette[index];

                if (IsBrushColor(color.R, color.G, color.B))
                {
                    opacity[index] = 0;
                }
                else
                {
                    opacity[index] = (byte)metadata.OpacityAlgorithm;
                }
            }
        }
        else if (IsAdditiveAlgorithm(metadata.OpacityAlgorithm))
        {
            for (int index = 0; index < palette.Count; index++)
            {
                opacity[index] = (byte)index;
            }
        }
        else
        {
            Array.Fill(opacity, byte.MaxValue);
        }

        opacity[metadata.TransparentColorIndex] = 0;

        return opacity;
    }
}
