using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // [맵 문자 규칙]
    // # = 벽
    // . = 빈칸
    // 1 = Player1 시작 위치
    // 2 = Player2 시작 위치
    // G = 골
    // T = 함정 (보라)
    // B = 공용 상자 (녹색)
    // Y = P1 전용 상자 (노란색)
    // O = P2 전용 상자 (주황색)
    // X = 철 상자 (빨간색, 아무도 못 밂)
    // a = 카메라 타입A (직선 1×3, 시계방향)
    // b = 카메라 타입B (직선 1×5, 시계방향)
    // c = 카메라 타입C (피라미드 3줄, 시계방향)
    // d = 카메라 타입D (피라미드 5줄, 시계방향)
    // e = 카메라 타입A (직선 1×3, 반시계방향)
    // f = 카메라 타입B (직선 1×5, 반시계방향)
    // g = 카메라 타입C (피라미드 3줄, 반시계방향)
    // h = 카메라 타입D (피라미드 5줄, 반시계방향)
    // s = 카메라 고정형 3×3 (본인 위치 포함, 비회전)
    // R = 로봇 적 (경로는 코드로 지정)
    // A = 동물 적
    // >, <, ^, v = 방향 지정 카메라 (시계방향 직선1×3)
    public sealed class MapLoader
    {
        public MapDefinition Load(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                throw new ArgumentNullException(nameof(textAsset));
            }

            return Load(textAsset.text);
        }

        public MapDefinition Load(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new ArgumentException("맵 텍스트가 비어있습니다.");
            }

            if (rawText.Length > 0 && rawText[0] == '\uFEFF')
            {
                rawText = rawText.Substring(1);
            }

            string[] lines = NormalizeLines(rawText);
            if (lines.Length == 0)
            {
                throw new InvalidOperationException("맵에 유효한 줄이 없습니다.");
            }

            int width = lines[0].Length;
            int height = lines.Length;
            CellFlags[] cellFlags = new CellFlags[width * height];
            List<SpawnData> spawns = new List<SpawnData>(16);

            bool hasPlayer1 = false;
            bool hasPlayer2 = false;

            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                if (line.Length != width)
                {
                    throw new InvalidOperationException(
                        $"맵 줄 너비 불일치: {y}행. 예상 {width}, 실제 {line.Length}.");
                }

                for (int x = 0; x < width; x++)
                {
                    char symbol = line[x];
                    GridPos position = new GridPos(x, y);
                    int index = (y * width) + x;

                    switch (symbol)
                    {
                        // ── 지형 ──
                        case '#':
                            cellFlags[index] = CellFlags.Wall;
                            break;

                        case '.':
                            cellFlags[index] = CellFlags.None;
                            break;

                        case 'G':
                            cellFlags[index] = CellFlags.Goal;
                            break;

                        case 'T':
                            cellFlags[index] = CellFlags.Trap;
                            break;

                        // ── 플레이어 ──
                        case '1':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnDataa(
                                EntityKind.Player, position, Direction.Down, playerSlot: 1));
                            hasPlayer1 = true;
                            break;

                        case '2':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.Player, position, Direction.Down, playerSlot: 2));
                            hasPlayer2 = true;
                            break;

                        // ── 상자 ──
                        case 'B':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.Box, position, Direction.None,
                                boxOwnership: BoxType.Shared));
                            break;

                        case 'Y':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.Box, position, Direction.None,
                                boxOwnership: BoxType.Player1Only));
                            break;

                        case 'O':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.Box, position, Direction.None,
                                boxOwnership: BoxType.Player2Only));
                            break;

                        case 'X':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.Box, position, Direction.None,
                                boxOwnership: BoxType.Iron));
                            break;

                        // ── 카메라 시계방향 (초기 방향: Up) ──
                        case 'a':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.LineShort));
                            break;

                        case 'b':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.LineLong));
                            break;

                        case 'c':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.PyramidSmall));
                            break;

                        case 'd':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.PyramidLarge));
                            break;

                        // ── 카메라 반시계방향 (초기 방향: Up) ──
                        case 'e':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.LineShort,
                                reverseRotation: true));
                            break;

                        case 'f':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.LineLong,
                                reverseRotation: true));
                            break;

                        case 'g':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.PyramidSmall,
                                reverseRotation: true));
                            break;

                        case 'h':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.PyramidLarge,
                                reverseRotation: true));
                            break;

                        // ── 카메라 고정형 3×3 (비회전) ──
                        case 's':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.None,
                                detectionPattern: CameraType.Fixed3x3));
                            break;
                        
                        // S = 고정형 3×3 (위 방향)
                        case 'S':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.Fixed3x3));
                            break;

                        // ── 로봇/동물 적 ──
                        case 'R':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.RobotEnemy, position, Direction.Right));
                            break;

                        case 'A':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.AnimalEnemy, position, Direction.Right));
                            break;

                        // ── 방향 지정 카메라 (시계방향 직선1×3) ──
                        case '>':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Right,
                                detectionPattern: CameraType.LineShort));
                            break;

                        case '<':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Left,
                                detectionPattern: CameraType.LineShort));
                            break;

                        case '^':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Up,
                                detectionPattern: CameraType.LineShort));
                            break;

                        case 'v':
                        case 'V':
                            cellFlags[index] = CellFlags.None;
                            spawns.Add(new SpawnData(
                                EntityKind.CameraEnemy, position, Direction.Down,
                                detectionPattern: CameraType.LineShort));
                            break;

                        default:
                            throw new InvalidOperationException(
                                $"알 수 없는 맵 기호 '{symbol}' at {position}.");
                    }
                }
            }

            if (!hasPlayer1 || !hasPlayer2)
            {
                throw new InvalidOperationException(
                    "맵에 Player1('1')과 Player2('2')가 모두 있어야 합니다.");
            }

            return new MapDefinition(width, height, cellFlags, spawns);
        }

        private static string[] NormalizeLines(string rawText)
        {
            string normalized = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] split = normalized.Split('\n');
            List<string> lines = new List<string>(split.Length);

            for (int i = 0; i < split.Length; i++)
            {
                string line = split[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string trimmedStart = line.TrimStart();
                if (trimmedStart.StartsWith("//"))
                {
                    continue;
                }

                lines.Add(line);
            }

            return lines.ToArray();
        }
    }
}