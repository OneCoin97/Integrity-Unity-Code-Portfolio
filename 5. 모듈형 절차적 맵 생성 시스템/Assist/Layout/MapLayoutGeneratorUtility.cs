using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
  {
      [Serializable]
      public class MapLayoutOption
      {
          public bool
              canMakeSecretRoomAtSecretRoad,
              canMakeSecretRoomOnLastDepth,
              canMakeForkAtSecretRoad,
              canMakeForkOnLastDepth,
              canMakeForkOnForkDepth,
              canMakeForkAtSecretRoomRoad,
              onlyOneSecret;

          [Range(0, 1)] public float
              pSecretRoadFromRoom,
              pSecretRoad;

          public int
              minRoomToRoomRoadLength = 2,
              maxRoomToRoomRoadLength = 4;

          public int maxForkOnDepth = 1; //해당값은 1로 고정 실제 맵생성은 이걸 전제하고 만듦
      }


      public class RandomUtility
      {
          public Random rng { get; private set; }

          public RandomUtility(int seed)
          {
              rng = new Random(seed);
          }

          public void shuffle<T>(List<T> list)
          {
              for (int i = list.Count - 1; i > 0; i--)
              {
                  int swapIndex = rng.Next(0, i + 1);
                  T temp = list[i];
                  list[i] = list[swapIndex];
                  list[swapIndex] = temp;
              }
          }

          public int getRandomValue(int min, int max)
          {
              if (min > max)
              {
                  max = min;
              }

              return rng.Next(min, max + 1);
          }


          public bool rollChance(float probability01)
          {
              if (float.IsNaN(probability01))
              {
                  return false;
              }

              if (probability01 <= 0f)
              {
                  return false;
              }

              if (probability01 >= 1f)
              {
                  return true;
              }

              double r = rng.NextDouble(); // 0.0 <= r < 1.0
              return r < probability01;
          }

      }

      public class WaitData
      {
          public MapLayoutDataEntry entry;
          public Vector2Int dir;
          public List<Vector2Int> neighborDir = new List<Vector2Int>(2);
          public int routeIndex;
          public int depth;
          public int lastRecoverDepth = -999;
          public int roadCount;
          public int startGift;
          public int startRecover;
          public bool isSecret;
          public bool haveSecretRoom;
          public bool haveSecretRoomInDepth;
          public bool isLastDepth;
          public bool isFork;

          public WaitData(MapLayoutDataEntry entry)
          {
              this.entry = entry;
          }
      }

      public class ForkCounter
      {
          private List<List<int>> forkData = new();

          public void initialize()
          {
              forkData.Clear();
          }

          public int getFork(int routeIndex, int depth)
          {
              ensureIndex(routeIndex, depth);
              return forkData[routeIndex][depth];
          }

          public void setFork(int routeIndex, int depth, int value)
          {
              ensureIndex(routeIndex, depth);
              forkData[routeIndex][depth] = value;
          }

          public void addFork(int routeIndex, int depth, int addValue)
          {
              ensureIndex(routeIndex, depth);
              forkData[routeIndex][depth] += addValue;
          }

          private void ensureIndex(int routeIndex, int depth)
          {
              if (routeIndex < 0 || depth < 0)
              {
                  // 음수 인덱스는 논리적으로 말이 안 됨. 조용히 무시하면 버그 못 잡음.
                  throw new System.ArgumentOutOfRangeException(
                      $"routeIndex({routeIndex}) and depth({depth}) must be >= 0");
              }

              // routeIndex까지 바깥 리스트 확장
              while (forkData.Count <= routeIndex)
              {
                  forkData.Add(new List<int>());
              }

              List<int> routeList = forkData[routeIndex];

              // depth까지 안쪽 리스트 확장 (기본값 0으로 채움)
              while (routeList.Count <= depth)
              {
                  routeList.Add(0);
              }
          }
      }
  }
