﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace aoc_runner
{
    public record Day20(Day20.Tile[] Tiles)
    {
        public Day20(string input) 
            : this(input.Split(Environment.NewLine+Environment.NewLine).Select(Tile.Parse).ToArray()) { }

        private readonly Tile[] _allPermutations = Tiles.SelectMany(x => x.AllPermutations()).ToArray();

        
        public long Part1()
        {
            var cornerTiles = _allPermutations.Where(i => !_allPermutations.Any(j => j.RightId == i.LeftId && j.TileId != i.TileId) &&
                                                          !_allPermutations.Any(j => j.TopId == i.BottomId && j.TileId != i.TileId))
                                              .ToArray();

            var ids = cornerTiles.Select(x => x.TileId).Distinct().ToArray();

            Debug.Assert(ids.Length == 4);

            return ids.Aggregate(1L, (agg, next) => agg * next);
        }
        
        public long Part2()
        {
            
            var topLeftCornerTiles = _allPermutations.Where(i => !_allPermutations.Any(j => j.RightId == i.LeftId && j.TileId != i.TileId) &&
                                                                !_allPermutations.Any(j => j.BottomId == i.TopId && j.TileId != i.TileId))
                                                    .ToArray();

            foreach (var tlct in topLeftCornerTiles)
            {
                var placedTiles = PlaceTiles(tlct);

                var gridWith = placedTiles.Keys.Max(i => i.x) + 1;
                var gridHeight = placedTiles.Keys.Max(i => i.y) + 1;
                
                char Grid(int x, int y) => placedTiles![(x / 8, y / 8)].Pixels[y % 8 + 1][x % 8 + 1];

                var seaMonster = new[]
                {
                    "                  # ".ToArray(),
                    "#    ##    ##    ###".ToArray(),
                    " #  #  #  #  #  #   ".ToArray()
                };

                bool IsSeaMonsterAt((int x, int y) location)
                {
                    for (var y = 0; y < 3; y++)
                    for (var x = 0; x < 20; x++)
                    {
                        if (seaMonster![y][x] == ' ') continue;
                        if (Grid(location.x + x, location.y + y) != '#') return false;
                    }

                    return true;
                }

                var seaMonsterLocations =
                    from x in Enumerable.Range(0, gridWith * 8 - 20)
                    from y in Enumerable.Range(0, gridHeight * 8 - 3)
                    let potentialLocation = (x, y)
                    where IsSeaMonsterAt(potentialLocation)
                    select potentialLocation;

                var seaMonsterCount = seaMonsterLocations.Count();

                if (seaMonsterCount > 0)
                {
                    var roughness = 0; 
                    
                    for (var y = 0; y < gridHeight*8; y++)
                    for (var x = 0; x < gridWith * 8; x++)
                    {
                        if (Grid(x, y) == '#') roughness++;
                    }

                    return roughness - seaMonsterCount * 15;
                }
            }

            return -1;
        }

        Dictionary<(int x, int y), Tile> PlaceTiles(Tile topLeftCorner)
        {
            var placedTiles = new Dictionary<(int x, int y), Tile>();
            bool IsPlaced(Tile tile) => placedTiles!.Values.Any(x => x.TileId == tile.TileId);
            
            placedTiles[(0, 0)] = topLeftCorner;

            var x = 1;
            var y = 0;
            while (true)
            {
                placedTiles.TryGetValue((x - 1, y), out var leftTile);
                placedTiles.TryGetValue((x, y - 1), out var topTile);

                if (leftTile == null && topTile == null)
                {
                    break;
                }

                var matchingTiles = _allPermutations.Where(i => (leftTile is null || i.LeftId == leftTile.RightId) &&
                                                                (topTile is null || i.TopId == topTile.BottomId))
                                                    .Where(i => !IsPlaced(i));

                var firstMatching = matchingTiles.FirstOrDefault();

                if (firstMatching is not null)
                {
                    placedTiles[(x, y)] = firstMatching;
                    x++;
                }
                else
                {
                    x = 0;
                    y++;
                }
            }

            return placedTiles;
        }
        
        public record Tile(int TileId, char[][] Pixels)
        {
            public static int Size = 10;

            public int TopId { get; } = GetId(Pixels, z => (z, 0));
            public int BottomId { get; } = GetId(Pixels, z => (z, Size - 1));
            public int LeftId { get; } = GetId(Pixels, z => (0, z));
            public int RightId { get; } = GetId(Pixels, z => (Size - 1, z));

            private static int GetId(char[][] pixels, Func<int, (int x, int y)> selectorFunc) => (
                from z in Enumerable.Range(0, Size)
                let coordinates = selectorFunc(z)
                let pixel = pixels[coordinates.y][coordinates.x]
                select (pixel == '#' ? 1 : 0) << z
            ).Aggregate(0, (acc, next) => acc | next);

            public Tile Transform(Action<int, int, char[][]> transformFunc)
            {
                var newPixels = Enumerable.Repeat(false, Size).Select(_ => new char[Size]).ToArray();

                for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    transformFunc(x, y, newPixels);
                }

                return new(TileId, newPixels);
            }

            public Tile TurnClockwise() => Transform((x, y, newPixels) => newPixels[x][Size - 1 - y] = Pixels[y][x]);
            public Tile Flip() => Transform((x, y, newPixels) => newPixels[y][Size - 1 - x] = Pixels[y][x]);

            public IEnumerable<Tile> AllPermutations()
            {
                var tile = this;
                for (var i = 1; i <= 8; i++)
                {
                    yield return tile;

                    if (i == 4)
                        tile = this.Flip();
                    else if (i == 8)
                        yield break;

                    tile = tile.TurnClockwise();
                }
            }

            public static Tile Parse(string ínput)
            {
                var lines = ínput.Split(Environment.NewLine);

                var pixels = lines.Skip(1).Select(l => l.ToArray()).ToArray();

                return new Tile(
                    int.Parse(Regex.Match(lines[0], @"Tile (\d*):").Groups[1].Value),
                    pixels
                );
            }

            public string Format()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Tile {TileId}:");
                foreach (var line in Pixels)
                    sb.AppendLine(new string(line));

                var nlSize = Environment.NewLine.Length;
                sb.Remove(sb.Length - nlSize, nlSize);
                return sb.ToString();
            }
        }
    }
}