using System.Collections.Generic;
using UnityEngine;

namespace MapBuild
{
    public class RouteMask
    {
        private List<List<Vector2Int>> up = new List<List<Vector2Int>>();
        private List<List<Vector2Int>> down = new List<List<Vector2Int>>();
        private List<List<Vector2Int>> left = new List<List<Vector2Int>>();
        private List<List<Vector2Int>> right = new List<List<Vector2Int>>();

        public RouteMask(int length)
        {
            if (length <= 0)
            {
                return;
            }

            // 1) Right 기준 후보 마스크들 생성
            right = createRightBaseMasksByLength(length);


            // 2) Right 기준을 각 방향으로 회전해서 저장
            for (int i = 0; i < right.Count; i++)
            {
                List<Vector2Int> baseMask = right[i];

                up.Add(copyAndRotateMask(baseMask, Vector2Int.up));
                left.Add(copyAndRotateMask(baseMask, Vector2Int.left));
                down.Add(copyAndRotateMask(baseMask, Vector2Int.down));
            }

        }


        public List<List<Vector2Int>> getMask(Vector2Int dir)
        {
            // 전제: dir은 Vector2Int.up / down / left / right 중 하나로만 전달된다.
            // 그 외 값이 들어올 경우 기본적으로 right 마스크를 반환한다.
            if (dir == Vector2Int.up)
            {
                return up;
            }

            if (dir == Vector2Int.down)
            {
                return down;
            }

            if (dir == Vector2Int.left)
            {
                return left;
            }

            return right;
        }


        // Right 기준 마스크 목록 생성
        // 규칙:
        // - maskOffsets 마지막 원소는 destination
        // - 그 전 원소들은 road
        // - 꺾임 1회 허용
        // - 꺾는 위치 turnIndex는 1 이상부터 (바로 꺾기 허용)
        private List<List<Vector2Int>> createRightBaseMasksByLength(int length)
        {
            List<List<Vector2Int>> masks = new List<List<Vector2Int>>();

            // 직진 마스크 1개
            // road: (1,0) ~ (length,0)
            // dest: (length+1, 0)
            List<Vector2Int> straightMask = new List<Vector2Int>();

            for (int i = 1; i <= length; i++)
            {
                straightMask.Add(new Vector2Int(i, 0));
            }

            straightMask.Add(new Vector2Int(length + 1, 0)); // destination
            masks.Add(straightMask);

            if (length < 2)
            {
                return masks;
            }

            // turnIndex는 "몇 칸 직진하고 꺾을지" (2 이상)
            // 꺾은 후 최소 1칸은 가야 하므로 turnIndex <= length - 1
            for (int turnIndex = 1; turnIndex <= length - 1; turnIndex++)
            {
                int afterLen = length - turnIndex; // 꺾은 뒤 road 칸 수 (최소 1)

                // 위로 꺾기 (Right 기준에서 up = +y)
                masks.Add(createTurnMaskOffsets(turnIndex, afterLen, true));

                // 아래로 꺾기
                masks.Add(createTurnMaskOffsets(turnIndex, afterLen, false));
            }

            return masks;
        }

        // Right 기준: turnIndex까지 직진 후 위/아래로 afterLen만큼 진행
        // 마지막 원소: destination(road 끝에서 한 칸 더)
        private List<Vector2Int> createTurnMaskOffsets(int turnIndex, int afterLen, bool turnUp)
        {
            List<Vector2Int> maskOffsets = new List<Vector2Int>();

            // 직진 구간 road
            for (int i = 1; i <= turnIndex; i++)
            {
                maskOffsets.Add(new Vector2Int(i, 0));
            }

            int ySign = turnUp ? 1 : -1;

            // 꺾은 뒤 구간 road
            for (int i = 1; i <= afterLen; i++)
            {
                maskOffsets.Add(new Vector2Int(turnIndex, ySign * i));
            }

            // destination은 road 끝에서 한 칸 더
            maskOffsets.Add(new Vector2Int(turnIndex, ySign * (afterLen + 1)));

            return maskOffsets;
        }

        // dir에 맞게 마스크 전체를 회전해서 복사
        private List<Vector2Int> copyAndRotateMask(List<Vector2Int> baseMask, Vector2Int dir)
        {
            List<Vector2Int> newMask = new List<Vector2Int>(baseMask.Count);

            for (int i = 0; i < baseMask.Count; i++)
            {
                Vector2Int rotated = rotateOffset(baseMask[i], dir);
                newMask.Add(rotated);
            }

            return newMask;
        }

        // Right 기준 오프셋을 dir 방향으로 회전
        private Vector2Int rotateOffset(Vector2Int offset, Vector2Int dir)
        {
            if (dir == Vector2Int.right)
            {
                return offset;
            }

            if (dir == Vector2Int.left)
            {
                return new Vector2Int(-offset.x, -offset.y);
            }

            if (dir == Vector2Int.up)
            {
                // (x,y) -> (-y, x)
                return new Vector2Int(-offset.y, offset.x);
            }

            // down
            // (x,y) -> (y, -x)
            return new Vector2Int(offset.y, -offset.x);
        }
    }
}