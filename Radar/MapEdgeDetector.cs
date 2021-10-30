using System;
using System.Collections.Generic;
using System.Linq;

namespace Radar
{
    /// <summary>
    /// Detects if the current map location is map edge or not.
    /// </summary>
    public class MapEdgeDetector
    {
        /// <summary>
        /// Map (current location) walkable data is stored in these variables.
        /// </summary>
        private readonly int currentTile, rightTile, leftTile, upTile, downTile;

        /// <summary>
        /// Class that helps with map edge detection.
        /// </summary>
        /// <param name="mapWalkableData">map data which stores the information about y,x being walkable or not.</param>
        /// <param name="bytesPerRow">number of bytes in a single row of walkable map data.</param>
        /// <param name="y">map y location whos edge caller wants to detect.</param>
        /// <param name="x">map x location whos edge caller wants to detect.</param>
        public MapEdgeDetector(byte[] mapWalkableData, int bytesPerRow, int y, int x)
        {
            var index = (y * bytesPerRow) + (x / 2); // (x / 2) => since there are 2 data points in 1 byte.
            var (oneIfFirstNibbleZeroIfNot, zeroIfFirstNibbleOneIfNot) = NibbleHandler(x);
            var shiftIfFirstNibble = oneIfFirstNibbleZeroIfNot * 0x4;
            var shiftIfSecondNibble = zeroIfFirstNibbleOneIfNot * 0x4;

            currentTile = SetTile(mapWalkableData, index, shiftIfSecondNibble);
            upTile = SetTile(mapWalkableData, index + bytesPerRow, shiftIfSecondNibble);
            downTile = SetTile(mapWalkableData, index - bytesPerRow, shiftIfSecondNibble);
            leftTile = SetTile(mapWalkableData, index - oneIfFirstNibbleZeroIfNot, shiftIfFirstNibble);
            rightTile = SetTile(mapWalkableData, index + zeroIfFirstNibbleOneIfNot, shiftIfFirstNibble);
        }

        private static (int oneIfFirstNibbleZeroIfNot, int zeroIfFirstNibbleOneIfNot) NibbleHandler(int x)
        {
            var wantsFirstNibble = x % 2 == 0;
            return wantsFirstNibble ? (1, 0) : (0, 1);
        }

        private static int SetTile(IEnumerable<byte> mapWalkableData, int index, int shiftAmount)
        {
            var data = mapWalkableData.ElementAtOrDefault(index);
            return (data >> shiftAmount) & 0xF;
        }


        /// <summary>
        /// Checks if the current tile is walkable and at least 1 other direction is walkable too.
        /// </summary>
        /// <returns></returns>
        public bool AtLeastOneDirectionIsBorder()
        {
            // we add the extra condition if currentTile == 1 to make the border thicker.
            return (!CanWalk(currentTile) || currentTile == 1) &&
                   (CanWalk(downTile) || CanWalk(upTile) || CanWalk(rightTile) || CanWalk(leftTile));
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

        /// <summary>
        /// Checks if (ImageX,ImageY) coordinate is within the width and height of the map.
        /// </summary>
        /// <param name="totalRows"></param>
        /// <param name="imageX"></param>
        /// <param name="imageY"></param>
        /// <param name="bytesPerRow"></param>
        /// <returns>True if X,Y is within the boundary of the image. Otherwise false</returns>
        public bool IsInsideMapBoundary(int totalRows, int imageX, int imageY, int bytesPerRow)
        {
            var width = bytesPerRow * 2;
            return imageX < width && imageX >= 0 && imageY < totalRows && imageY >= 0;
        }
    }
}