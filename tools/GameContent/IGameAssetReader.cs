namespace GameContent;

public interface IGameAssetReader
{
    AssetSprite? Read(GameAssetDescriptor.Sprite descriptor);

    AssetAnimation? Read(GameAssetDescriptor.Animation descriptor);

    AssetSound? Read(GameAssetDescriptor.Sound descriptor);
}
