using System;
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;

namespace Smugglers
{
    public record ModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.cptsparkler.smugglers0";
        public override string Name { get; init; } = "Smugglers";
        public override string Author { get; init; } = "CptSparkler";
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, global::SemanticVersioning.Range>? ModDependencies { get; init; }
        public override string? Url { get; init; }
        public override string? License { get; init; } = "MIT";
        public override bool? IsBundleMod { get; init; } = false;
    }
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class Smugglers(
        ISptLogger<Smugglers> logger,
        ModHelper modHelper,
        CustomItemService customItemService,
        DatabaseService databaseService)
        : IOnLoad
    {
        public Task OnLoad()
        {
            var database = databaseService.GetTables();
            var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

            // Load JSON files using strongly-typed deserialization
            var looseLootData = modHelper.GetJsonDataFromFile<Dictionary<string, List<SpawnData>>>(pathToMod, @"data\caseSpawns.json");
            //var questData = modHelper.GetJsonDataFromFile<Dictionary<string, List<Quest>>>(pathToMod, @"data\smugglerQuest.json");
            //var smugglerbot = modHelper.GetJsonDataFromFile<Dictionary<string, List<BotData>>>(pathToMod, @"data\smuggerBot.json");

            logger.Info("[Smugglers] Start loading items");

            var itemService = new SmugglersItemService(logger, customItemService, database);

            itemService.AddToLooseLoot(looseLootData);
            // might go elsewhere to inject at start
            //var questService = new SmugglersQuestService(logger, database);

            //questService.AddToQuest(questData);
            // might go elsewhere to inject at start
            //var botService = new SmugglersBotService(logger, database);

            //botService.AddToLooseLoot(smugglerbot);

            logger.Success("[Smugglers] Finished loading items");
            return Task.CompletedTask;
        }
    }
}