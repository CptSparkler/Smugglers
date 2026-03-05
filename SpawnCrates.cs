using System;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services.Mod;

namespace Smugglers
{
    internal class SpawnDataPosition
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
    }
    internal class SpawnDataRotation
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
    }
    internal class LooseLootItem
    {
        public string? _tpl { get; set; }
        public string? _id { get; set; }
        public double relativeProbability { get; set; }
    }

    //internal class GroupData
    //{
    //    public double Position  { get; set; }
    //    public double Rotation { get; set; }
    //}
    internal class SpawnData
    {
        public string? Id { get; set; }
        public SpawnDataPosition Position { get; set; }
        public SpawnDataRotation Rotation { get; set; }
        //public List<GroupData>? GroupPositions { get; set; }
        public double probability { get; set; }
        public List<LooseLootItem> Items { get; set; }
    }
    internal class SmugglersItemService
    {
        private readonly ISptLogger<Smugglers> _logger;
        private readonly DatabaseTables _database;

        public SmugglersItemService(ISptLogger<Smugglers> logger, DatabaseTables database)
        {
            _logger = logger;
            _database = database;
        }

        public SmugglersItemService(ISptLogger<Smugglers> logger, CustomItemService customItemService, DatabaseTables database)
        {
            _logger = logger;
            _database = database;
        }

        private Spawnpoint CreateLooseLootSpawn(SpawnData spawnData)
        {
            var spawn = new Spawnpoint
            {
                LocationId = $"({spawnData.Position.x}, {spawnData.Position.y}, {spawnData.Position.z})",
                Probability = spawnData.probability,
                Template = new SpawnpointTemplate
                {
                    Id = spawnData.Id,
                    IsContainer = false,
                    UseGravity = false,
                    RandomRotation = false,
                    Position = new XYZ { X = spawnData.Position.x, Y = spawnData.Position.y, Z = spawnData.Position.z },
                    Rotation = new XYZ { X = spawnData.Rotation.x, Y = spawnData.Rotation.y, Z = spawnData.Rotation.z },
                    IsAlwaysSpawn = true,
                    IsGroupPosition = false,
                    GroupPositions = new List<GroupPosition>(),
                    Root = "",
                    Items = spawnData.Items.Select(item => new SptLootItem
                    {
                        ComposedKey = item._id,
                        Id = new MongoId(),
                        Template = new MongoId(item._tpl),
                        Upd = new Upd { StackObjectsCount = 1 }
                    }).ToList()
                },
                ItemDistribution = spawnData.Items.Select(item => new LooseLootItemDistribution
                {
                    ComposedKey = new ComposedKey { Key = item._id },
                    RelativeProbability = item.relativeProbability
                }).ToList()
            };

            return spawn;
        }
        public void AddToLooseLoot(Dictionary<string, List<SpawnData>> looseLootData)
        {
            var lootChangesByMap = new Dictionary<string, List<Spawnpoint>>();

            foreach ((string mapName, List<SpawnData> mapLoot) in looseLootData)
            {
                string propertyMapName = _database.Locations.GetMappedKey(mapName);

                if (!_database.Locations.GetDictionary().ContainsKey(propertyMapName))
                    continue;

                Location location = _database.Locations.GetDictionary()[propertyMapName];

                foreach (SpawnData spawnData in mapLoot)
                {
                    Spawnpoint looseLootSpawn = CreateLooseLootSpawn(spawnData);

                    if (!lootChangesByMap.ContainsKey(propertyMapName))
                        lootChangesByMap[propertyMapName] = new List<Spawnpoint>();

                    lootChangesByMap[propertyMapName].Add(looseLootSpawn);
                }
            }

            foreach ((string propertyMapName, List<Spawnpoint> changes) in lootChangesByMap)
            {
                if (!_database.Locations.GetDictionary().TryGetValue(propertyMapName, out Location location))
                {
                    _logger.Warning($"Map {propertyMapName} not found in database.");
                    continue;
                }

                if (location.LooseLoot == null)
                {
                    _logger.Warning($"LooseLoot is null for {propertyMapName}");
                    continue;
                }

                location.LooseLoot.AddTransformer(lazyLoadedLooseLoot =>
                {
                    var currentSpawnpoints = lazyLoadedLooseLoot.Spawnpoints?.ToList() ?? new List<Spawnpoint>();

                    foreach (Spawnpoint spawnpoint in changes)
                    {
                        currentSpawnpoints.Add(spawnpoint);
                    }

                    return lazyLoadedLooseLoot with { Spawnpoints = currentSpawnpoints };
                });
            }
        }
    }
}


