using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 맵 텍스트(.txt)를 파싱하여 MapDefinition을 생성한다.
    // 기존의 switch-case 30여 개를 제거하고,
    // MapSymbolRegistrySO를 통해 기호별 매핑을 외부에서 관리한다.

    public sealed class MapLoader
    {
        private readonly MapSymbolRegistrySO _registry;

        // registry: 기호 매핑 레지스트리 (StageManager에서 주입)
        public MapLoader(MapSymbolRegistrySO registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry),
                    "MapSymbolRegistrySO가 null입니다. " +
                    "StageManager 인스펙터에서 Symbol Registry를 연결해주세요.");
            }

            _registry = registry;
        }

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

            // BOM 제거
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

                    if (!_registry.TryGetEntry(symbol, out MapSymbolEntry entry))
                    {
                        throw new InvalidOperationException(
                            $"등록되지 않은 맵 기호 '{symbol}' at ({x}, {y}). " +
                            $"MapSymbolRegistrySO에 이 기호를 추가해주세요.");
                    }

                    // 1) 지형 적용
                    cellFlags[index] = entry.cellFlags;

                    // 2) 엔티티 스폰
                    if (entry.spawnsEntity)
                    {
                        if (entry.entityConfig == null)
                        {
                            Debug.LogWarning(
                                $"[MapLoader] 기호 '{symbol}'({entry.description}): " +
                                $"spawnsEntity=true인데 entityConfig가 null입니다. " +
                                $"at ({x}, {y})");
                            continue;
                        }

                        SpawnData spawn = CreateSpawnData(entry, position);
                        spawns.Add(spawn);
                    }
                }
            }

            // 플레이어 검증
            ValidatePlayers(spawns);

            return new MapDefinition(width, height, cellFlags, spawns);
        }

        // EntityConfigSO와 MapSymbolEntry의 facing으로 SpawnData를 생성한다.
        private static SpawnData CreateSpawnData(MapSymbolEntry entry, GridPos position)
        {
            EntityConfigSO config = entry.entityConfig;

            return new SpawnData(
                config.kind,
                position,
                entry.facing,
                playerSlot: config.usePlayerData ? config.playerSlot : 0,
                boxOwnership: config.useBoxData ? config.boxOwnership : BoxType.Shared,
                detectionPattern: config.useCameraData ? config.cameraPattern : CameraType.LineShort,
                reverseRotation: config.useCameraData && config.reverseRotation
            );
        }

        // Player1과 Player2가 모두 존재하는지 검증한다.
        private static void ValidatePlayers(List<SpawnData> spawns)
        {
            bool hasPlayer1 = false;
            bool hasPlayer2 = false;

            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i].Kind != EntityKind.Player)
                {
                    continue;
                }

                if (spawns[i].PlayerSlot == 1) hasPlayer1 = true;
                if (spawns[i].PlayerSlot == 2) hasPlayer2 = true;
            }

            if (!hasPlayer1 || !hasPlayer2)
            {
                throw new InvalidOperationException(
                    "맵에 Player1('1')과 Player2('2')가 모두 있어야 합니다. " +
                    "MapSymbolRegistrySO에서 플레이어 기호가 올바르게 등록되어 있는지 확인해주세요.");
            }
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