namespace Packer;

internal sealed partial class WuQuantizer
{
    private sealed class Box
    {
        public int RedMin;
        public int RedMax;
        public int GreenMin;
        public int GreenMax;
        public int BlueMin;
        public int BlueMax;
        public int AlphaMin;
        public int AlphaMax;
        public int Volume;

        public int Index0000;
        public int Index0001;
        public int Index0010;
        public int Index0011;
        public int Index0100;
        public int Index0101;
        public int Index0110;
        public int Index0111;
        public int Index1000;
        public int Index1001;
        public int Index1010;
        public int Index1011;
        public int Index1100;
        public int Index1101;
        public int Index1110;
        public int Index1111;

        public void Update()
        {
            Volume = (RedMax - RedMin) * (GreenMax - GreenMin) * (BlueMax - BlueMin) * (AlphaMax - AlphaMin);

            Index0000 = GetIndex(RedMin, GreenMin, BlueMin, AlphaMin);
            Index0001 = GetIndex(RedMin, GreenMin, BlueMin, AlphaMax);
            Index0010 = GetIndex(RedMin, GreenMin, BlueMax, AlphaMin);
            Index0011 = GetIndex(RedMin, GreenMin, BlueMax, AlphaMax);
            Index0100 = GetIndex(RedMin, GreenMax, BlueMin, AlphaMin);
            Index0101 = GetIndex(RedMin, GreenMax, BlueMin, AlphaMax);
            Index0110 = GetIndex(RedMin, GreenMax, BlueMax, AlphaMin);
            Index0111 = GetIndex(RedMin, GreenMax, BlueMax, AlphaMax);
            Index1000 = GetIndex(RedMax, GreenMin, BlueMin, AlphaMin);
            Index1001 = GetIndex(RedMax, GreenMin, BlueMin, AlphaMax);
            Index1010 = GetIndex(RedMax, GreenMin, BlueMax, AlphaMin);
            Index1011 = GetIndex(RedMax, GreenMin, BlueMax, AlphaMax);
            Index1100 = GetIndex(RedMax, GreenMax, BlueMin, AlphaMin);
            Index1101 = GetIndex(RedMax, GreenMax, BlueMin, AlphaMax);
            Index1110 = GetIndex(RedMax, GreenMax, BlueMax, AlphaMin);
            Index1111 = GetIndex(RedMax, GreenMax, BlueMax, AlphaMax);
        }
    }
}
