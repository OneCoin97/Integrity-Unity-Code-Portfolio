using UnityEngine;

namespace MapBuild
{
    public enum Dir
    {
        Right,
        Left,
        Up,
        Down,
    }
    public static class DirUtility
    {
        public static int getRestLength(Dir dir, Vector2Int cPos, Vector2Int end)
        {
            switch (dir)
            {
                case Dir.Right:
                case Dir.Left:
                    return Mathf.Abs(end.x - cPos.x);

                case Dir.Up:
                case Dir.Down:
                    return Mathf.Abs(end.y - cPos.y);

                default:
                    return 0;
            }
        }
        
        public static bool isUpDir(Dir dir, Dir forkDir)
        {
            Dir upDir = getLeftDir(dir);
            return forkDir == upDir;
        }
        public static bool isSameAxis(Dir dir, Dir otherDir)
        {
            return dir.Equals(otherDir) || dir.Equals(getReverseDir(otherDir));
        }
        public static Vector2Int getOffset(Dir dir, Vector2Int customOffset, int sizeX, int sizeZ)
        {
            Vector2Int result = getOffset(dir, customOffset, sizeZ);
            
            if (isReverseDir(dir))
            {
                result += getDirVector(dir) * (sizeX - 1);
            }
            
            return result;
        }
        
        public static Vector2Int getOffset(Dir dir, Vector2Int customOffset, int sizeZ)
        {
            Vector2Int result = -getRightAngleDirVector(dir) * (sizeZ / 2);
            
            result += getDirVector(dir) * customOffset.x;
            result += getRightAngleDirVector(dir) * customOffset.y;
            
            return result;
        }
        public static Dir getDirFromVector(Vector2Int dirVector)
        {
            if (dirVector == new Vector2Int(1, 0))
            {
                return Dir.Right;
            }

            if (dirVector == new Vector2Int(-1, 0))
            {
                return Dir.Left;
            }

            if (dirVector == new Vector2Int(0, 1))
            {
                return Dir.Up;
            }

            if (dirVector == new Vector2Int(0, -1))
            {
                return Dir.Down;
            }

            // 예상 밖 입력 (대각선, (0,0), 기타)
            return Dir.Right; // 기본값은 프로젝트 규칙에 맞게 바꿔도 됨
        }
        public static Vector2Int getDirVector(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return new Vector2Int(1, 0);

                case Dir.Left:
                    return new Vector2Int(-1, 0);

                case Dir.Up:
                    return new Vector2Int(0, 1);

                case Dir.Down:
                    return new Vector2Int(0, -1);

                default:
                    return Vector2Int.zero;
            }
        }

        public static Dir getForwardDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Dir.Right;

                case Dir.Up:
                    return Dir.Up;

                case Dir.Left:
                    return Dir.Right;

                case Dir.Down:
                    return Dir.Up;

                default:
                    return Dir.Right; // 기본값(애매하면 Right로)
            }
        }

      
        public static Dir getReverseDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Dir.Left;

                case Dir.Up:
                    return Dir.Down;

                case Dir.Left:
                    return Dir.Right;

                case Dir.Down:
                    return Dir.Up;

                default:
                    return Dir.Right; // 기본값(애매하면 Right로)
            }
        }

        public static Dir getRightAngleDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Dir.Up;

                case Dir.Up:
                    return Dir.Right;

                case Dir.Left:
                    return Dir.Up;

                case Dir.Down:
                    return Dir.Right;

                default:
                    return Dir.Right; // 기본값(애매하면 Right로)
            }
        }

        public static Vector2Int getLeftDirVector(Dir dir)
        {
            return getDirVector(getLeftDir(dir));
        }

        public static Dir getLeftDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Dir.Up;

                case Dir.Up:
                    return Dir.Left;

                case Dir.Left:
                    return Dir.Down;

                case Dir.Down:
                    return Dir.Right;

                default:
                    return Dir.Right; // 기본값(애매하면 Right로)
            }
        }

        public static bool isReverseDir(Dir dir)
        {
            return dir.Equals(Dir.Left) || dir.Equals(Dir.Down);
        }

        public static Vector2Int getRightAngleDirVector(Dir dir)
        {
            Dir leftDir = getRightAngleDir(dir);
            return getDirVector(leftDir);
        }

        public static Quaternion getDirRotation(Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Quaternion.identity;

                case Dir.Up:
                    return Quaternion.Euler(0f, -90f, 0f);

                case Dir.Left:
                    return Quaternion.Euler(0f, 180f, 0f);

                case Dir.Down:
                    return Quaternion.Euler(0f, 90f, 0f);

                default:
                    return Quaternion.identity;
            }
        }
    }


}