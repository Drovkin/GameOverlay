using System.Linq;

namespace Radar
{
    /// <summary>
    /// Detects if the current map location is map edge or not.
    /// </summary>
    public class MapEdgeDetector
    {
        private readonly int bytesPerRow;
        private readonly byte[] mapWalkableData;

        /// <summary>
        /// Class that helps with map edge detection.
        /// </summary>
        /// <param name="mapWalkableData">Byte array that contains map walkable data</param>
        /// <param name="bytesPerRow"></param>
        public MapEdgeDetector(byte[] mapWalkableData, int bytesPerRow)
        {
            this.mapWalkableData = mapWalkableData;
            this.bytesPerRow = bytesPerRow;
        }

        /// <summary>
        /// Detects if the current tile is a border
        /// By detecting if the current tile is not walkable and at least 1 other direction is walkable.
        /// </summary>
        /// <returns>True if the current tile is a border, false otherwise.</returns>
        public bool IsBorder(int x, int y)
        {
            var index = (y * bytesPerRow) + (x / 2); // (x / 2) => since there are 2 data points in 1 byte.
            var (oneIfFirstNibbleZeroIfNot, zeroIfFirstNibbleOneIfNot) = NibbleHandler(x);
            var shiftIfFirstNibble = oneIfFirstNibbleZeroIfNot * 0x4;
            var shiftIfSecondNibble = zeroIfFirstNibbleOneIfNot * 0x4;

            var currentTile = SetTile(index, shiftIfSecondNibble);

            // we add the extra condition if currentTile != 1 to make the border thicker.
            if (currentTile != 1 && CanWalk(currentTile))
            {
                return false;
            }

            var upTile = SetTile(index + bytesPerRow, shiftIfSecondNibble);
            if (CanWalk(upTile))
            {
                return true;
            }

            var downTile = SetTile(index - bytesPerRow, shiftIfSecondNibble);
            if (CanWalk(downTile))
            {
                return true;
            }

            var leftTile = SetTile(index - oneIfFirstNibbleZeroIfNot, shiftIfFirstNibble);
            if (CanWalk(leftTile))
            {
                return true;
            }

            var rightTile = SetTile(index + zeroIfFirstNibbleOneIfNot, shiftIfFirstNibble);
            return CanWalk(rightTile);
        }


        /// <summary>
        /// Checks if (ImageX,ImageY) coordinate is within the width and height of the map.
        /// </summary>
        /// <param name="totalRows"></param>
        /// <param name="imageX"></param>
        /// <param name="imageY"></param>
        /// <returns>True if X,Y is within the boundary of the image. Otherwise false</returns>
        public bool IsInsideMapBoundary(int totalRows, int imageX, int imageY)
        {
            var width = bytesPerRow * 2;
            return imageX < width && imageX >= 0 && imageY < totalRows && imageY >= 0;
        }

        /// <summary>
        /// 0 = not walkable 1,2,3,4,5 means potentially walkable.
        /// It's potentially walkable because it also depends on entity size
        /// (e.g. if entity size is 1 then 1 or above is walkable and
        /// if entity size is 3 than 3 or above is walkable). For the purpose
        /// of generating map we will assume everything above 0 is walkable.
        /// </summary>
        /// <param name="tileValue">map tile walkable value</param>
        /// <returns></returns>
        private static bool CanWalk(int tileValue)
        {
            return tileValue != 0;
        }

        private static (int oneIfFirstNibbleZeroIfNot, int zeroIfFirstNibbleOneIfNot) NibbleHandler(int x)
        {
            var wantsFirstNibble = x % 2 == 0;
            return wantsFirstNibble ? (1, 0) : (0, 1);
        }

        private int SetTile(int index, int shiftAmount)
        {
            var data = mapWalkableData.ElementAtOrDefault(index);
            return (data >> shiftAmount) & 0xF;
        }
    }
}