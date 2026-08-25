using System;
using UnityEngine;

namespace MapBuild
{
    /// <summary>
    /// RoadPartRandom 데이터를 기반으로
    /// 실제 블록 단위의 도로 구조를 맵에 설치하는 저수준 빌더.
    ///
    /// - 직선 도로(buildRoad)
    /// - T자/좌우 분기(buildForkWithDir / buildForkLeftRight)
    /// - 시크릿 경로(buildSecretRoad)
    /// - 브릿지, 포켓, 덮어쓰기(overwrite) 처리
    ///
    /// MapBuilder가 상위에서 레이아웃 흐름을 제어하고,
    /// RoadBuilder는 개별 도로 파츠를 실제 BlockMap으로 병합(merge)하는 역할만 담당한다.
    ///
    /// 즉, 이 클래스는 "도로 배치 전용 조립기"이며
    /// 레이아웃 판단이나 그래프 해석 책임은 가지지 않는다.
    /// </summary>
    public class RoadBuilder : MonoBehaviour
    {
        private BlockMapManager blockMapManager;
        private RoadPartRandom roadPart;
        private Map cMap;

        public void initialize(BlockMapManager blockMapManager, Map map)
        {
            
            this.blockMapManager = blockMapManager;
            this.cMap = map;
        }

        public void setData(RoadPartRandom roadPart)
        {
            if (roadPart != null)
            {
                this.roadPart = roadPart;
            }
            else
            {
                Debug.LogError("RoadBuilder.setData() : roadPart is null");
            }
        }

        public Vector2Int buildRoad(Vector2Int start, Dir dir, int length, bool startBridge, bool endBridge,
            bool withPocket = false)
        {
            // 첫 칸은 기존 연결 타일과 겹치므로 실제 전진 길이에서 제외한다.
            int restLength = length - 1;

            if (restLength <= 0)
            {
                return start;
            }

            if (startBridge)
            {
                if (roadPart.bridge != null && restLength > roadPart.bridge.getSizeX() - roadPart.bridge.overWriteRange)
                {
                    start = mergeMap(start, dir, roadPart.bridge.offset, roadPart.bridge.map, ref restLength, true);
                    start -= DirUtility.getDirVector(dir) * roadPart.bridge.overWriteRange;
                    restLength += roadPart.bridge.overWriteRange;
                }
            }

            if (endBridge)
            {
                if (roadPart.bridge != null && restLength > roadPart.bridge.getSizeX())
                {
                    restLength -= roadPart.bridge.map.blockMap.sizeX - roadPart.bridge.overWriteRange - 1;
                }
                else
                {
                    endBridge = false;
                }
            }

            if (withPocket && roadPart.pathPocket != null && restLength > roadPart.pathPocket.getSizeX())
            {
                restLength -= roadPart.pathPocket.getSizeX();
                int halfLength = restLength / 2;
                restLength -= halfLength;

                int tmp = 0;

                start = makeRoad(start, dir, halfLength);

                start = mergeMap(start, dir, roadPart.pathPocket.offset, roadPart.pathPocket.map, ref tmp);

                start = makeRoad(start, dir, restLength);
            }
            else if (restLength > 0)
            {
                start = makeRoad(start, dir, restLength);
            }

            if (endBridge)
            {
                start -= DirUtility.getDirVector(dir) * roadPart.bridge.overWriteRange;
                start = mergeMap(start, dir, roadPart.bridge.offset, roadPart.bridge.map, ref restLength);
            }

            return start;
     
        }

        public (Vector2Int, Vector2Int) buildForkWithDir(Vector2Int start, Dir dir, Dir forkDir, int length,
            bool startBridge)
        {
            int half;
            Vector2Int forkPos = installForkBase(start, dir, length, startBridge, out half);

            Vector2Int exit = DirUtility.getDirVector(dir);
            Vector2Int forkExit = DirUtility.getDirVector(forkDir);

            if (length <= roadPart.fork.inner + 1)
            {
                buildRoad(forkPos, DirUtility.getReverseDir(dir), roadPart.fork.inner + 1, false, false);
            }

            return (forkPos + exit * half, forkPos + forkExit * half);
        }

        public (Vector2Int, Vector2Int) buildForkLeftRight(Vector2Int start, Dir dir, int length, bool startBridge)
        {
            int half;
            Vector2Int forkPos = installForkBase(start, dir, length, startBridge, out half);

            Dir leftDir = DirUtility.getLeftDir(dir);
            Vector2Int leftExit = DirUtility.getDirVector(leftDir);
            Vector2Int rightExit = DirUtility.getDirVector(DirUtility.getReverseDir(leftDir));

            return (forkPos + leftExit * half, forkPos + rightExit * half);
        }

        public (Vector2Int, Vector2Int) buildSecretRoad(Vector2Int start, Dir dir, Dir forkDir, int length,
            bool startBridge)
        {
            int restLength = length;

            bool isReverse = !forkDir.Equals(DirUtility.getLeftDir(dir));
            Vector2Int forkPos = roadPart.secretRoad.getForkPos(isReverse);
            Vector2Int realForkPos = roadPart.secretRoad.forkPos;

            restLength = Mathf.Clamp(restLength, forkPos.x, Int32.MaxValue);

            restLength -= forkPos.x;

            Vector2Int installPos = start + restLength * DirUtility.getDirVector(dir);
            Vector2Int secretPos = installPos;

            int temp = roadPart.secretRoad.getSizeX();
            Vector2Int bottomOffset = roadPart.secretRoad.offset;

            restLength += roadPart.secretRoad.overWriteRange;

            bool isForkReverse = DirUtility.isReverseDir(forkDir);

            if (isForkReverse)
            {
                secretPos -= (realForkPos.y / 2 + 1) * DirUtility.getRightAngleDirVector(dir);

                if (bottomOffset.y > 0)
                {
                    if (roadPart.secretRoad.getSizeZ() % 2 == 0)
                    {
                        bottomOffset = (-bottomOffset.y + 1) * Vector2Int.up;
                    }
                    else
                    {
                        bottomOffset = (-bottomOffset.y) * Vector2Int.up;
                    }

                }
            }
            else
            {
                secretPos += (realForkPos.y / 2 + 1) * DirUtility.getRightAngleDirVector(dir);
            }

            if (dir.Equals(Dir.Up) || dir.Equals(Dir.Left))
            {
                isForkReverse = !isForkReverse;
            }

            int oddOffset = 1;

            if (isForkReverse)
            {
                secretPos += (forkPos.x + oddOffset) * DirUtility.getDirVector(dir);
            }
            else
            {
                secretPos += (forkPos.x - oddOffset) * DirUtility.getDirVector(dir);
            }

            Vector2Int end = mergeMap(installPos, dir, bottomOffset, roadPart.secretRoad.map, ref temp, isReverse);

            if (restLength > 0)
            {
                if (startBridge)
                {
                    if (roadPart.bridge != null &&
                        restLength > roadPart.bridge.getSizeX() - roadPart.bridge.overWriteRange)
                    {
                        start = mergeMap(start, dir, roadPart.bridge.offset, roadPart.bridge.map, ref restLength, true);
                        start -= DirUtility.getDirVector(dir) * roadPart.bridge.overWriteRange;
                        restLength += roadPart.bridge.overWriteRange;
                    }
                }

                if (restLength > 0)
                {
                    makeRoad(start, dir, restLength);
                }
            }

            return (end - DirUtility.getDirVector(dir) * roadPart.secretRoad.startOverWriteRange, secretPos);
        }

        private Vector2Int installForkBase(Vector2Int start, Dir dir, int length, bool startBridge, out int half)
        {
            int restLength = length;

            Vector2Int forkPos = start + DirUtility.getDirVector(dir) * (restLength - roadPart.fork.inner - 1);

            blockMapManager.addBlockMap(
                roadPart.fork.map.blockMap,
                forkPos + DirUtility.getOffset(dir, roadPart.fork.offset, roadPart.fork.getSizeX(),
                    roadPart.fork.getSizeZ()),
                DirUtility.getDirRotation(dir)
            );

            forkPos += DirUtility.getDirVector(dir) * roadPart.fork.inner;

            if (startBridge)
            {
                if (roadPart.bridge != null && restLength > roadPart.bridge.getSizeX() - roadPart.bridge.overWriteRange)
                {
                    start = mergeMap(start, dir, roadPart.bridge.offset, roadPart.bridge.map, ref restLength, true);
                    start -= DirUtility.getDirVector(dir) * roadPart.bridge.overWriteRange;
                    restLength += roadPart.bridge.overWriteRange;
                }
            }

            restLength -= roadPart.fork.inner + roadPart.fork.overWriteRange + 1;

            if (restLength > 0)
            {
                makeRoad(start, dir, restLength);
            }

            half = roadPart.fork.inner + roadPart.fork.startOverWriteRange;
            return forkPos;
        }

        private Vector2Int mergeMap(Vector2Int start, Dir dir, Vector2Int bottomOffset, Map otherMap, ref int length,
            bool reverse = false)
        {
            Vector2Int offset =
                DirUtility.getOffset(dir, bottomOffset, otherMap.blockMap.sizeX, otherMap.blockMap.sizeZ);
            Vector2Int temp = start + offset;
            Vector3Int position;
            Quaternion quaternion;

            if (reverse)
            {
                quaternion = DirUtility.getDirRotation(DirUtility.getReverseDir(dir));
            }
            else
            {
                quaternion = DirUtility.getDirRotation(dir);
            }

            position = new Vector3Int(temp.x, 0, temp.y);

            blockMapManager.addBlockMap(otherMap.blockMap, position, quaternion);

            cMap.merge(otherMap, position, quaternion);

            length -= otherMap.blockMap.sizeX;

            return start + DirUtility.getDirVector(dir) * (otherMap.blockMap.sizeX);
        }

        private Vector2Int makeRoad(Vector2Int start, Dir dir, int length)
        {
            if (length <= 0)
            {
                return start;
            }
            int roadLength = roadPart.main.getSizeX();
            int restLength = length;

            BlockMap blockMap = roadPart.main.map.blockMap;
            Vector2Int dirVector = DirUtility.getDirVector(dir);
            Quaternion quaternion = DirUtility.getDirRotation(dir);

            Vector2Int offset = DirUtility.getOffset(dir, roadPart.main.offset, roadPart.main.getSizeX(),
                roadPart.main.getSizeZ());

            while (restLength >= roadLength)
            {
                blockMapManager.addBlockMap(blockMap, start + offset, quaternion);
                start += dirVector * roadLength;
                restLength -= roadLength;
            }

            if (restLength > 0)
            {
                BlockMap cutMap = blockMap.createSubBlockMap(
                    Vector2Int.zero,
                    DirUtility.getDirVector(Dir.Right) * restLength +
                    DirUtility.getDirVector(Dir.Up) * roadPart.main.map.blockMap.sizeZ
                );
                
                offset = DirUtility.getOffset(dir, roadPart.main.offset, cutMap.sizeX, cutMap.sizeZ);

                blockMapManager.addBlockMap(cutMap, start + offset, quaternion);
            }

            start += dirVector * restLength;

            return start;
        }
    }
}



