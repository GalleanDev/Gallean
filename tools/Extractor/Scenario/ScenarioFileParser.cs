namespace Extractor;

internal sealed class ScenarioFileParser(ScenarioRecordReader reader, ScenarioFileFormat format, Db.ScenarioId scenarioId)
{
    private readonly record struct CityData(
        Db.Id CityId,
        Db.Id Owner,
        Db.Id Subrace,
        Db.Id? Stack,
        int PositionX,
        int PositionY);

    private readonly record struct SiteData(
        Db.Id SiteId,
        int ImageIso,
        string? ImageInterface,
        Db.Id? VisiterId,
        int PositionX,
        int PositionY,
        int AiPriority);

    private const string ScenarioInfoObject = ".?AVCScenarioInfo@@";
    private const string PlayerObject = ".?AVCMidPlayer@@";
    private const string PlayerBuildingsObject = ".?AVCPlayerBuildings@@";
    private const string PlayerKnownSpellsObject = ".?AVCPlayerKnownSpells@@";
    private const string UnitObject = ".?AVCMidUnit@@";
    private const string StackObject = ".?AVCMidStack@@";
    private const string StackTemplateObject = ".?AVCMidStackTemplate@@";
    private const string StackDestroyedObject = ".?AVCMidStackDestroyed@@";
    private const string ItemObject = ".?AVCMidItem@@";
    private const string CapitalObject = ".?AVCCapital@@";
    private const string VillageObject = ".?AVCMidVillage@@";
    private const string RuinObject = ".?AVCMidRuin@@";
    private const string BagObject = ".?AVCMidBag@@";
    private const string CrystalObject = ".?AVCMidCrystal@@";
    private const string LandmarkObject = ".?AVCMidLandmark@@";
    private const string LocationObject = ".?AVCMidLocation@@";
    private const string RoadObject = ".?AVCMidRoad@@";
    private const string MountainsObject = ".?AVCMidMountains@@";
    private const string EventObject = ".?AVCMidEvent@@";
    private const string SubraceObject = ".?AVCMidSubRace@@";
    private const string DiplomacyObject = ".?AVCMidDiplomacy@@";
    private const string MapObject = ".?AVCMidgardMap@@";
    private const string MapBlockObject = ".?AVCMidgardMapBlock@@";
    private const string MapFogObject = ".?AVCMidgardMapFog@@";
    private const string PlanObject = ".?AVCMidgardPlan@@";
    private const string ScenarioVariablesObject = ".?AVCMidScenVariables@@";
    private const string TalismanChargesObject = ".?AVCMidTalismanCharges@@";
    private const string SpellEffectsObject = ".?AVCMidSpellEffects@@";
    private const string SpellCastObject = ".?AVCMidSpellCast@@";
    private const string QuestLogObject = ".?AVCMidQuestLog@@";
    private const string TurnSummaryObject = ".?AVCTurnSummary@@";
    private const string RodObject = ".?AVCMidRod@@";
    private const string TombObject = ".?AVCMidTomb@@";
    private const string SiteMageObject = ".?AVCMidSiteMage@@";
    private const string SiteMercenariesObject = ".?AVCMidSiteMercs@@";
    private const string SiteMerchantObject = ".?AVCMidSiteMerchant@@";
    private const string SiteTrainerObject = ".?AVCMidSiteTrainer@@";
    private const string SiteResourceMarketObject = ".?AVCMidSiteResourceMarket@@";

    private const string BeginObjectMarker = "BEGOBJECT\0";
    private const string EndObjectMarker = "ENDOBJECT\0";

    private const int UnnamedCountNameLength = 10;
    private const int MapBlockByteCount = 128;

    private readonly List<Db.ScenarioBag> bags = [];
    private readonly List<Db.ScenarioCrystal> crystals = [];
    private readonly List<Db.ScenarioEventCondition> eventConditions = [];
    private readonly List<Db.ScenarioEventEffect> eventEffects = [];
    private readonly List<Db.ScenarioEvent> events = [];
    private readonly List<Db.ScenarioEventUnfog> eventUnfogs = [];
    private readonly List<Db.ScenarioFog> fogs = [];
    private readonly List<Db.ScenarioFortification> fortifications = [];
    private readonly List<Db.ScenarioInfo> infos = [];
    private readonly List<Db.ScenarioInventory> inventories = [];
    private readonly List<Db.ScenarioItem> items = [];
    private readonly List<Db.ScenarioLandmark> landmarks = [];
    private readonly List<Db.ScenarioLocation> locations = [];
    private readonly List<Db.ScenarioMage> mages = [];
    private readonly List<Db.ScenarioMapBlock> mapBlocks = [];
    private readonly List<Db.ScenarioMapElement> mapElements = [];
    private readonly List<Db.ScenarioMercenary> mercenaries = [];
    private readonly List<Db.ScenarioMerchant> merchants = [];
    private readonly List<Db.ScenarioMountain> mountains = [];
    private readonly List<Db.ScenarioObjectMap> objectMap = [];
    private readonly List<Db.ScenarioPlayer> players = [];
    private readonly List<Db.ScenarioRelation> relations = [];
    private readonly List<Db.ScenarioRoad> roads = [];
    private readonly List<Db.ScenarioRod> rods = [];
    private readonly List<Db.ScenarioRuin> ruins = [];
    private readonly List<Db.ScenarioSite> sites = [];
    private readonly List<Db.ScenarioSpell> spells = [];
    private readonly List<Db.ScenarioStack> stacks = [];
    private readonly List<Db.ScenarioStackTemplateModifier> stackTemplateModifiers = [];
    private readonly List<Db.ScenarioStackTemplate> stackTemplates = [];
    private readonly List<Db.ScenarioSubrace> subraces = [];
    private readonly List<Db.ScenarioTalismanCharges> talismanCharges = [];
    private readonly List<Db.TextScenario> texts = [];
    private readonly List<Db.ScenarioUnitGroup> unitGroups = [];
    private readonly List<Db.ScenarioUnitModifier> unitModifiers = [];
    private readonly List<Db.ScenarioUnit> units = [];
    private readonly List<Db.ScenarioVariable> variables = [];

    public ScenarioFileTables Parse(int objectCount)
    {
        for (int i = 0; i < objectCount; i++)
        {
            string what = reader.ReadString("WHAT");
            Db.Id objectId = reader.ReadId("OBJ_ID");

            reader.ExpectName(BeginObjectMarker);
            ReadObject(what, objectId);
            reader.ExpectName(EndObjectMarker);

            objectMap.Add(new Db.ScenarioObjectMap(scenarioId, objectId, what));
        }

        return new ScenarioFileTables
        {
            ScenarioBags = bags,
            ScenarioCrystals = crystals,
            ScenarioEvents = events,
            ScenarioEventConditions = eventConditions,
            ScenarioEventEffects = eventEffects,
            ScenarioEventUnfogs = eventUnfogs,
            ScenarioFogs = fogs,
            ScenarioFortifications = fortifications,
            ScenarioInfos = infos,
            ScenarioInventories = inventories,
            ScenarioItems = items,
            ScenarioLandmarks = landmarks,
            ScenarioLocations = locations,
            ScenarioMages = mages,
            ScenarioMapBlocks = mapBlocks,
            ScenarioMapElements = mapElements,
            ScenarioMercenaries = mercenaries,
            ScenarioMerchants = merchants,
            ScenarioMountains = mountains,
            ScenarioObjectMaps = objectMap,
            ScenarioPlayers = players,
            ScenarioRelations = relations,
            ScenarioRoads = roads,
            ScenarioRods = rods,
            ScenarioRuins = ruins,
            ScenarioSites = sites,
            ScenarioSpells = spells,
            ScenarioStacks = stacks,
            ScenarioStackTemplates = stackTemplates,
            ScenarioStackTemplateModifiers = stackTemplateModifiers,
            ScenarioSubraces = subraces,
            ScenarioTalismanCharges = talismanCharges,
            ScenarioUnits = units,
            ScenarioUnitGroups = unitGroups,
            ScenarioUnitModifiers = unitModifiers,
            ScenarioVariables = variables,
            TextScenarios = texts
        };
    }

    private void ReadObject(string what, Db.Id objectId)
    {
        switch (what)
        {
            case ScenarioInfoObject:
                ReadScenarioInfo();
                break;
            case PlayerObject:
                ReadPlayer();
                break;
            case PlayerBuildingsObject:
                ReadPlayerBuildings();
                break;
            case PlayerKnownSpellsObject:
                ReadPlayerKnownSpells(objectId);
                break;
            case UnitObject:
                ReadUnit();
                break;
            case StackObject:
                ReadStack();
                break;
            case StackTemplateObject:
                ReadStackTemplate();
                break;
            case ItemObject:
                ReadItem();
                break;
            case CapitalObject:
                ReadCapital();
                break;
            case VillageObject:
                ReadVillage();
                break;
            case RuinObject:
                ReadRuin();
                break;
            case BagObject:
                ReadBag();
                break;
            case CrystalObject:
                ReadCrystal();
                break;
            case LandmarkObject:
                ReadLandmark();
                break;
            case LocationObject:
                ReadLocation();
                break;
            case RoadObject:
                ReadRoad();
                break;
            case MountainsObject:
                ReadMountains();
                break;
            case EventObject:
                ReadEvent();
                break;
            case SubraceObject:
                ReadSubrace();
                break;
            case DiplomacyObject:
                ReadDiplomacy();
                break;
            case MapBlockObject:
                ReadMapBlock();
                break;
            case MapFogObject:
                ReadMapFog(objectId);
                break;
            case PlanObject:
                ReadPlan();
                break;
            case ScenarioVariablesObject:
                ReadScenarioVariables();
                break;
            case TalismanChargesObject:
                ReadTalismanCharges();
                break;
            case SiteMageObject:
                ReadMageSite();
                break;
            case SiteMercenariesObject:
                ReadMercenarySite();
                break;
            case SiteMerchantObject:
                ReadMerchantSite();
                break;
            case SiteTrainerObject:
                ReadTrainerSite();
                break;
            case SiteResourceMarketObject:
                ReadResourceMarketSite();
                break;
            case MapObject:
                reader.ReadUnnamedCount(UnnamedCountNameLength);
                break;
            case SpellEffectsObject:
                ReadSpellEffects();
                break;
            case SpellCastObject:
                ReadSpellCast();
                break;
            case QuestLogObject:
                ReadQuestLog();
                break;
            case TurnSummaryObject:
                ReadTurnSummary();
                break;
            case StackDestroyedObject:
                ReadStackDestroyed();
                break;
            case TombObject:
                ReadTomb();
                break;
            case RodObject:
                ReadRod();
                break;
            default:
                throw new InvalidDataException($"Unknown scenario object '{what}' at offset {reader.Position}.");
        }
    }

    private void ReadScenarioInfo()
    {
        Db.Id infoId = reader.ReadId("INFO_ID");
        Db.Id campaign = reader.ReadId("CAMPAIGN");
        bool sourceM = reader.ReadBool("SOURCE_M");
        int quantityCities = reader.ReadInt("QTY_CITIES");
        string name = reader.ReadString("NAME");
        string? description = reader.ReadOptionalString("DESC");
        string briefing = reader.ReadString("BRIEFING");
        string defeatWin = reader.TryReadString("DEBUNKW") ?? string.Empty;
        string? defeatWin2 = reader.TryReadOptionalString("DEBUNKW2");
        string? defeatWin3 = reader.TryReadOptionalString("DEBUNKW3");
        string? defeatWin4 = reader.TryReadOptionalString("DEBUNKW4");
        string? defeatWin5 = reader.TryReadOptionalString("DEBUNKW5");
        string defeatLose = reader.ReadString("DEBUNKL");
        string briefLong1 = reader.TryReadString("BRIEFLONG1") ?? string.Empty;
        string? briefLong2 = reader.TryReadOptionalString("BRIEFLONG2");
        string? briefLong3 = reader.TryReadOptionalString("BRIEFLONG3");
        string? briefLong4 = reader.TryReadOptionalString("BRIEFLONG4");
        string? briefLong5 = reader.TryReadOptionalString("BRIEFLONG5");
        bool o = reader.TryReadBool("O") ?? false;
        int currentTurn = reader.ReadInt("CUR_TURN");
        int maxUnit = reader.ReadInt("MAX_UNIT");
        int maxSpell = reader.ReadInt("MAX_SPELL");
        int maxLeader = reader.ReadInt("MAX_LEADER");
        int maxCity = reader.ReadInt("MAX_CITY");
        int mapSize = reader.ReadInt("MAP_SIZE");
        Db.DifficultyType difficultyScene = (Db.DifficultyType)reader.ReadInt("DIFFSCEN");
        Db.DifficultyType difficultyGame = (Db.DifficultyType)reader.ReadInt("DIFFGAME");
        string creator = reader.ReadString("CREATOR");

        int[] playerSlots = new int[13];
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i] = reader.ReadInt($"PLAYER_{i + 1}");
        }

        int suggestedLevel = reader.ReadInt("SUGG_LVL");
        int mapSeed = reader.TryReadInt("MAP_SEED") ?? 0;

        infos.Add(new Db.ScenarioInfo(
            scenarioId,
            infoId,
            currentTurn,
            mapSeed,
            mapSize,
            quantityCities,
            creator,
            sourceM,
            difficultyGame,
            difficultyScene,
            campaign,
            maxUnit,
            maxLeader,
            maxCity,
            maxSpell,
            playerSlots[0],
            playerSlots[1],
            playerSlots[2],
            playerSlots[3],
            playerSlots[4],
            playerSlots[5],
            playerSlots[6],
            playerSlots[7],
            playerSlots[8],
            playerSlots[9],
            playerSlots[10],
            playerSlots[11],
            playerSlots[12],
            name,
            description,
            briefing,
            briefLong1,
            briefLong2,
            briefLong3,
            briefLong4,
            briefLong5,
            defeatWin,
            defeatWin2,
            defeatWin3,
            defeatWin4,
            defeatWin5,
            defeatLose,
            suggestedLevel,
            o));
    }

    private void ReadPlayer()
    {
        Db.Id playerId = reader.ReadId("PLAYER_ID");
        AddText(playerId, Db.TextScenario.NameTextId, 0, reader.ReadString("NAME_TXT"));
        AddText(playerId, Db.TextScenario.DescriptionTextId, 0, reader.ReadString("DESC_TXT"));
        Db.Id lordId = reader.ReadId("LORD_ID");
        Db.Id raceId = reader.ReadId("RACE_ID");
        Db.Id fogId = reader.ReadId("FOG_ID");
        Db.Id knownId = reader.ReadId("KNOWN_ID");
        Db.Id buildsId = reader.ReadId("BUILDS_ID");
        int face = reader.ReadInt("FACE");
        int quantityBreaks = reader.ReadInt("QTY_BREAKS");
        Db.ResourceSet bank = reader.ReadBank("BANK");
        bool isHuman = reader.ReadBool("IS_HUMAN");
        Db.ResourceSet spellBank = reader.TryReadBank("SPELL_BANK") ?? GetDefaultSpellBank(bank, isHuman);
        Db.AiAttitudeType attitude = (Db.AiAttitudeType)reader.ReadInt("ATTITUDE");
        int researchTurn = reader.ReadInt("RESEAR_T");
        int constructTurn = reader.ReadInt("CONSTR_T");
        Db.Id? spy1 = reader.ReadOptionalId("SPY_1");
        Db.Id? spy2 = reader.ReadOptionalId("SPY_2");
        Db.Id? spy3 = reader.ReadOptionalId("SPY_3");
        Db.Id? capturedBy = reader.TryReadOptionalId("CAPT_BY");

        bool? alwaysAi = null;
        if (format.HasDesignData)
        {
            alwaysAi = reader.ReadBool("ALWAYSAI");
        }

        Db.Id?[] exchangeMapIds = [null, null, null];
        int[] exchangeMapTurns = [0, 0, 0];

        if (reader.HasField("EXMAPID1"))
        {
            for (int i = 0; i < 3; i++)
            {
                exchangeMapIds[i] = reader.ReadOptionalId($"EXMAPID{i + 1}");
                exchangeMapTurns[i] = ParseInt(reader.ReadString($"EXMAPTURN{i + 1}"));
            }
        }

        players.Add(new Db.ScenarioPlayer(
            scenarioId,
            playerId,
            raceId,
            lordId,
            fogId,
            researchTurn,
            constructTurn,
            buildsId,
            knownId,
            bank,
            spellBank,
            attitude,
            face,
            spy1,
            spy2,
            spy3,
            quantityBreaks,
            isHuman,
            capturedBy,
            alwaysAi,
            exchangeMapIds[0],
            exchangeMapIds[1],
            exchangeMapIds[2],
            exchangeMapTurns[0],
            exchangeMapTurns[1],
            exchangeMapTurns[2]));
    }

    private void ReadPlayerBuildings()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("BUILD_ID");
        }
    }

    private void ReadPlayerKnownSpells(Db.Id objectId)
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            spells.Add(new Db.ScenarioSpell(scenarioId, objectId, reader.ReadId("SPELL_ID")));
        }
    }

    private void ReadUnit()
    {
        Db.Id unitId = reader.ReadId("UNIT_ID");
        Db.Id typeId = reader.ReadId("TYPE");
        int level = reader.ReadInt("LEVEL");
        int modifierCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < modifierCount; i++)
        {
            unitModifiers.Add(new Db.ScenarioUnitModifier(scenarioId, unitId, reader.ReadId("MODIF_ID")));
        }

        int creation = reader.ReadInt("CREATION");
        string name = reader.ReadString("NAME_TXT");
        bool transformed = reader.ReadBool("TRANSF");

        if (transformed)
        {
            reader.ReadString("ORIGTYPEID");
            reader.ReadBool("KEEP_HP");
            reader.ReadInt("ORIG_XP");
            reader.ReadInt("HP_BEFORE");
            reader.ReadInt("HP_BEF_MAX");
        }

        bool? dynamicLevel = reader.TryReadBool("DYNLEVEL");
        int hitPoints = reader.ReadInt("HP");
        int experience = reader.ReadInt("XP");

        AddText(unitId, Db.TextScenario.NameTextId, 0, name);
        units.Add(new Db.ScenarioUnit(scenarioId, unitId, hitPoints, experience, creation, typeId, level, transformed, dynamicLevel));
    }

    private void ReadStack()
    {
        Db.Id groupId = ReadUnitGroup();
        int itemCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        Db.Id[] stackItems = new Db.Id[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            stackItems[i] = reader.ReadId("ITEM_ID");
        }

        Db.Id stackId = reader.ReadId("STACK_ID");
        Db.Id? sourceTemplateId = reader.TryReadOptionalId("SRCTMPL_ID");
        Db.Id leaderId = reader.ReadId("LEADER_ID");
        bool leaderAlive = reader.ReadBool("LEADR_ALIV");
        (int positionX, int positionY) = reader.ReadPosition();
        int morale = reader.ReadInt("MORALE");
        int move = reader.ReadInt("MOVE");
        int facing = reader.ReadInt("FACING");
        Db.Id? banner = reader.ReadOptionalId("BANNER");
        Db.Id? tome = reader.ReadOptionalId("TOME");
        Db.Id? battle1 = reader.ReadOptionalId("BATTLE1");
        Db.Id? battle2 = reader.ReadOptionalId("BATTLE2");
        Db.Id? artifact1 = reader.ReadOptionalId("ARTIFACT1");
        Db.Id? artifact2 = reader.ReadOptionalId("ARTIFACT2");
        Db.Id? boots = reader.ReadOptionalId("BOOTS");
        Db.Id owner = reader.ReadId("OWNER");
        Db.Id? inside = reader.ReadOptionalId("INSIDE");
        Db.Id subrace = ReadSubraceId(stackId);
        bool invisible = reader.ReadBool("INVISIBLE");
        bool aiIgnore = reader.ReadBool("AI_IGNORE");
        int upgradeCount = reader.ReadInt("UPGCOUNT");
        int order = reader.ReadInt("ORDER");
        Db.Id? orderTarget = reader.ReadOptionalId("ORDER_TARG");

        Db.OrderType aiOrder;
        Db.Id? aiOrderTarget;

        if (reader.HasField("AIORDER"))
        {
            aiOrder = (Db.OrderType)reader.ReadInt("AIORDER");
            aiOrderTarget = reader.ReadOptionalId("AIORDERTAR");
        }
        else
        {
            aiOrder = Db.OrderType.Normal;
            aiOrderTarget = null;
        }

        int aiPriority = reader.ReadInt("AIPRIORITY");
        int creationLevel = reader.ReadInt("CREAT_LVL");
        int numberOfBattles = reader.ReadInt("NBBATTLE");

        CheckUnitGroupOwner(groupId, stackId);
        AddInventory(stackId, stackItems);

        stacks.Add(new Db.ScenarioStack(
            scenarioId,
            stackId,
            sourceTemplateId,
            positionX,
            positionY,
            leaderId,
            leaderAlive,
            morale,
            move,
            facing,
            banner,
            tome,
            battle1,
            battle2,
            artifact1,
            artifact2,
            boots,
            owner,
            subrace,
            inside,
            invisible,
            upgradeCount,
            aiIgnore,
            aiPriority,
            (Db.OrderType)order,
            orderTarget,
            creationLevel,
            numberOfBattles,
            aiOrder,
            aiOrderTarget));
    }

    private void ReadStackTemplate()
    {
        Db.Id templateId = reader.ReadId("ID");
        Db.Id owner = reader.ReadId("OWNER");
        Db.Id leader = reader.ReadId("LEADER");
        int leaderLevel = reader.ReadInt("LEADER_LVL");
        AddText(templateId, Db.TextScenario.NameTextId, 0, reader.ReadString("NAME_TXT"));
        Db.Id? orderTarget = reader.ReadOptionalId("ORDER_TARG");
        Db.Id subrace = ReadSubraceId(templateId);
        Db.OrderType order = (Db.OrderType)reader.ReadInt("ORDER");

        Db.Id?[] templateUnits = new Db.Id?[6];
        int[] templateLevels = new int[6];

        for (int i = 0; i < 6; i++)
        {
            templateUnits[i] = reader.ReadOptionalId($"UNIT_{i}");
            templateLevels[i] = reader.ReadInt($"UNIT_{i}_LVL");
        }

        int[] positions = new int[6];
        for (int i = 0; i < 6; i++)
        {
            positions[i] = reader.ReadInt($"POS_{i}");
        }

        bool useFacing = reader.ReadBool("USE_FACING");
        int facing = reader.ReadInt("FACING");
        int modifierCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < modifierCount; i++)
        {
            if (!reader.HasField("UNIT_POS"))
            {
                break;
            }

            int unitPosition = reader.ReadInt("UNIT_POS");
            Db.Id modifierId = reader.ReadId("MODIF_ID");

            stackTemplateModifiers.Add(new Db.ScenarioStackTemplateModifier(scenarioId, templateId, modifierId, unitPosition));
        }

        int aiPriority;
        if (format.HasDesignData && reader.HasField("AIPRIORITY"))
        {
            aiPriority = reader.ReadInt("AIPRIORITY");
        }
        else
        {
            aiPriority = 3;
        }

        Db.Id? firstUnit = templateUnits[0];
        if (firstUnit is null)
        {
            throw new InvalidDataException($"Stack template '{templateId}' has no unit in its first slot.");
        }

        stackTemplates.Add(new Db.ScenarioStackTemplate(
            scenarioId,
            templateId,
            leader,
            leaderLevel,
            owner,
            subrace,
            firstUnit.Value,
            templateLevels[0],
            templateUnits[1],
            templateLevels[1],
            templateUnits[2],
            templateLevels[2],
            templateUnits[3],
            templateLevels[3],
            templateUnits[4],
            templateLevels[4],
            templateUnits[5],
            templateLevels[5],
            positions[0],
            positions[1],
            positions[2],
            positions[3],
            positions[4],
            positions[5],
            order,
            orderTarget,
            useFacing,
            facing,
            aiPriority));
    }

    private void ReadItem()
    {
        Db.Id itemId = reader.ReadId("ITEM_ID");
        Db.Id itemType = reader.ReadId("ITEM_TYPE");

        items.Add(new Db.ScenarioItem(scenarioId, itemId, itemType));
    }

    private void ReadCapital()
    {
        CityData city = ReadCity();
        int itemCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < itemCount; i++)
        {
            inventories.Add(new Db.ScenarioInventory(scenarioId, city.CityId, reader.ReadId("ITEM_ID")));
        }

        int aiPriority = reader.ReadInt("AIPRIORITY");

        fortifications.Add(CreateFortification(city, aiPriority));
    }

    private void ReadVillage()
    {
        CityData city = ReadCity();
        int itemCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < itemCount; i++)
        {
            inventories.Add(new Db.ScenarioInventory(scenarioId, city.CityId, reader.ReadId("ITEM_ID")));
        }

        int aiPriority = reader.ReadInt("AIPRIORITY");
        Db.Id? protectBuilding = reader.ReadOptionalId("PROTECT_B");
        int regenBonus = reader.ReadInt("REGEN_B");
        int morale = reader.ReadInt("MORALE");
        int growthTurn = reader.ReadInt("GROWTH_T");
        int size = reader.ReadInt("SIZE");
        bool protectedFromUndead = reader.ReadBool("P_O_UN");
        bool protectedFromHeretic = reader.ReadBool("P_O_HE");
        bool protectedFromHuman = reader.ReadBool("P_O_HU");
        bool protectedFromDwarf = reader.ReadBool("P_O_DW");
        bool? protectedFromElf = reader.TryReadBool("P_O_EL");
        int riotTurn = reader.ReadInt("RIOT_T");

        fortifications.Add(CreateFortification(city, aiPriority) with
        {
            Size = size,
            RegenerationBonus = regenBonus,
            ProtectionBuildingId = protectBuilding,
            Morale = morale,
            GrowthTurn = growthTurn,
            ProtectedFromUndead = protectedFromUndead,
            ProtectedFromHeretic = protectedFromHeretic,
            ProtectedFromHuman = protectedFromHuman,
            ProtectedFromDwarf = protectedFromDwarf,
            ProtectedFromElf = protectedFromElf,
            RiotTurns = riotTurn
        });
    }

    private void ReadRuin()
    {
        Db.Id ruinId = reader.ReadId("RUIN_ID");
        AddText(ruinId, Db.TextScenario.RuinTitleTextId, 0, reader.ReadString("TITLE"));
        AddText(ruinId, Db.TextScenario.RuinDescriptionTextId, 0, reader.ReadString("DESC"));
        int image = reader.ReadInt("IMAGE");
        (int positionX, int positionY) = reader.ReadPosition();
        string cash = reader.ReadString("CASH");
        Db.Id? item = reader.ReadOptionalId("ITEM");
        Db.Id? looter = reader.ReadOptionalId("LOOTER");
        int aiPriority = reader.ReadInt("AIPRIORITY");
        int visitorCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < visitorCount; i++)
        {
            reader.ReadString("RUIN_ID");
            reader.ReadString("VISITER");
        }

        CheckUnitGroupOwner(ReadUnitGroup(), ruinId);

        ruins.Add(new Db.ScenarioRuin(scenarioId, ruinId, image, positionX, positionY, cash, item, looter, aiPriority));
    }

    private void ReadBag()
    {
        Db.Id bagId = reader.ReadId("BAG_ID");
        (int positionX, int positionY) = reader.ReadPosition();
        int image = reader.ReadInt("IMAGE");
        int aiPriority = reader.ReadInt("AIPRIORITY");
        int itemCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < itemCount; i++)
        {
            inventories.Add(new Db.ScenarioInventory(scenarioId, bagId, reader.ReadId("ITEM_ID")));
        }

        bags.Add(new Db.ScenarioBag(scenarioId, bagId, positionX, positionY, null, image, aiPriority));
    }

    private void ReadCrystal()
    {
        Db.Id crystalId = reader.ReadId("CRYSTAL_ID");
        int resource = reader.ReadInt("RESOURCE");
        (int positionX, int positionY) = reader.ReadPosition();
        int aiPriority = reader.ReadInt("AIPRIORITY");

        crystals.Add(new Db.ScenarioCrystal(scenarioId, crystalId, resource, positionX, positionY, aiPriority));
    }

    private void ReadLandmark()
    {
        Db.Id landmarkId = reader.ReadId("LMARK_ID");
        Db.Id typeId = reader.ReadId("TYPE");
        (int positionX, int positionY) = reader.ReadPosition();

        if (format.HasDesignData && reader.HasField("DESC_TXT"))
        {
            AddText(landmarkId, Db.TextScenario.DescriptionTextId, 0, reader.ReadString("DESC_TXT"));
        }

        landmarks.Add(new Db.ScenarioLandmark(scenarioId, landmarkId, typeId, positionX, positionY, null));
    }

    private void ReadLocation()
    {
        Db.Id locationId = reader.ReadId("LOC_ID");
        (int positionX, int positionY) = reader.ReadPosition();
        string name = reader.ReadString("NAME_TXT");
        int radius = reader.ReadInt("RADIUS");

        AddText(locationId, Db.TextScenario.NameTextId, 0, name);
        locations.Add(new Db.ScenarioLocation(scenarioId, locationId, positionX, positionY, radius));
    }

    private void ReadRoad()
    {
        Db.Id roadId = reader.ReadId("ROAD_ID");
        int index = reader.ReadInt("INDEX");
        int variant = reader.TryReadInt("VAR") ?? 0;
        (int positionX, int positionY) = reader.ReadPosition();

        roads.Add(new Db.ScenarioRoad(scenarioId, roadId, positionX, positionY, index, variant));
    }

    private void ReadMountains()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            int mountainId = reader.ReadInt("ID_MOUNT");
            (int sizeX, int sizeY) = reader.ReadSize();
            (int positionX, int positionY) = reader.ReadPosition();
            int image = reader.ReadInt("IMAGE");
            int race = reader.ReadInt("RACE");

            mountains.Add(new Db.ScenarioMountain(scenarioId, mountainId, sizeX, sizeY, positionX, positionY, image, race));
        }
    }

    private void ReadSubrace()
    {
        Db.Id subraceId = reader.ReadId("SUBRACE_ID");
        int subrace = reader.ReadInt("SUBRACE");
        Db.Id playerId = reader.ReadId("PLAYER_ID");
        reader.ReadInt("NUMBER");
        AddText(subraceId, Db.TextScenario.NameTextId, 0, reader.ReadString("NAME_TXT"));
        int banner = reader.TryReadInt("BANNER") ?? 0;

        subraces.Add(new Db.ScenarioSubrace(scenarioId, subraceId, playerId, subrace, banner));
    }

    private void ReadDiplomacy()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            Db.RaceType race1 = (Db.RaceType)reader.ReadInt("RACE_1");
            Db.RaceType race2 = (Db.RaceType)reader.ReadInt("RACE_2");
            int relation = reader.ReadInt("RELATION");

            relations.Add(new Db.ScenarioRelation(scenarioId, race1, race2, relation));
        }
    }

    private void ReadMapBlock()
    {
        Db.Id blockId = reader.ReadId("BLOCKID");
        int byteCount = reader.ReadInt("BLOCKDATA");

        if (byteCount != MapBlockByteCount)
        {
            throw new InvalidDataException($"Map block '{blockId}' has {byteCount} bytes instead of {MapBlockByteCount}.");
        }

        mapBlocks.Add(new Db.ScenarioMapBlock(scenarioId, blockId, SixbitTextEncoder.Encode(reader.ReadBytes(byteCount))));
    }

    private void ReadMapFog(Db.Id objectId)
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            int positionY = reader.ReadInt("POS_Y");
            int byteCount = reader.ReadInt("FOG");

            fogs.Add(new Db.ScenarioFog(scenarioId, objectId, positionY, SixbitTextEncoder.Encode(reader.ReadBytes(byteCount))));
        }
    }

    private void ReadPlan()
    {
        reader.ReadUnnamedCount(UnnamedCountNameLength);
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            (int positionX, int positionY) = reader.ReadPosition();
            Db.Id elementId = reader.ReadId("ELEMENT");

            mapElements.Add(new Db.ScenarioMapElement(scenarioId, positionX, positionY, elementId));
        }
    }

    private void ReadScenarioVariables()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            int variableId = reader.ReadInt("ID");
            string name = reader.ReadString("NAME");
            int value = reader.ReadInt("VALUE");

            variables.Add(new Db.ScenarioVariable(scenarioId, variableId, name, value));
        }
    }

    private void ReadTalismanCharges()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            Db.Id talismanId = reader.ReadId("ID_TALIS");
            int charges = reader.ReadInt("CHARGES");

            talismanCharges.Add(new Db.ScenarioTalismanCharges(scenarioId, talismanId, charges));
        }
    }

    private void ReadMageSite()
    {
        SiteData site = ReadSite();
        int spellCount = reader.ReadInt("QTY_SPELL");

        for (int i = 0; i < spellCount; i++)
        {
            mages.Add(new Db.ScenarioMage(scenarioId, site.SiteId, reader.ReadId("SPELL_ID")));
        }

        ReadSiteVisitors();

        sites.Add(CreateSite(site) with { QuantitySpell = spellCount });
    }

    private void ReadMercenarySite()
    {
        SiteData site = ReadSite();
        int unitCount = reader.ReadInt("QTY_UNIT");

        for (int i = 0; i < unitCount; i++)
        {
            Db.Id unitId = reader.ReadId("UNIT_ID");
            int unitLevel = reader.ReadInt("UNIT_LEVEL");
            bool unique = reader.ReadBool("UNIT_UNIQ");

            mercenaries.Add(new Db.ScenarioMercenary(scenarioId, site.SiteId, unitId, unitLevel, unique));
        }

        ReadSiteVisitors();

        sites.Add(CreateSite(site) with { QuantityUnit = unitCount });
    }

    private void ReadMerchantSite()
    {
        SiteData site = ReadSite();
        bool buyArmor = reader.ReadBool("BUY_ARMOR");
        bool buyJewel = reader.ReadBool("BUY_JEWEL");
        bool buyWeapon = reader.ReadBool("BUY_WEAPON");
        bool buyBanner = reader.ReadBool("BUY_BANNER");
        bool buyPotion = reader.ReadBool("BUY_POTION");
        bool buyScroll = reader.ReadBool("BUY_SCROLL");
        bool buyWand = reader.ReadBool("BUY_WAND");
        bool buyValue = reader.ReadBool("BUY_VALUE");
        int itemCount = reader.ReadInt("QTY_ITEM");

        for (int i = 0; i < itemCount; i++)
        {
            Db.Id itemId = reader.ReadId("ITEM_ID");
            int count = reader.ReadInt("ITEM_COUNT");

            merchants.Add(new Db.ScenarioMerchant(scenarioId, site.SiteId, itemId, count));
        }

        bool mission = reader.ReadBool("MISSION");

        ReadSiteVisitors();

        sites.Add(CreateSite(site) with
        {
            QuantityItem = itemCount,
            BuyArmor = buyArmor,
            BuyJewel = buyJewel,
            BuyWeapon = buyWeapon,
            BuyBanner = buyBanner,
            BuyPotion = buyPotion,
            BuyScroll = buyScroll,
            BuyWand = buyWand,
            BuyValue = buyValue,
            Mission = mission
        });
    }

    private void ReadTrainerSite()
    {
        SiteData site = ReadSite();

        ReadSiteVisitors();

        sites.Add(CreateSite(site));
    }

    private void ReadResourceMarketSite()
    {
        SiteData site = ReadSite();

        reader.ReadBool("CUSTOM");
        reader.ReadString("BANK");
        reader.ReadInt("INF");
        ReadSiteVisitors();

        sites.Add(CreateSite(site));
    }

    private void ReadSpellEffects()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("UNIT_ID");
            reader.ReadString("ORIGIN_ID");
            reader.ReadString("MODIF_ID");
            reader.ReadString("CASTER_ID");
            reader.ReadInt("TURN_STOP");
        }
    }

    private void ReadSpellCast()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("PLAYER_ID");
            reader.ReadString("ORIGIN");
            reader.ReadInt("TURN_STOP");
        }

        reader.ReadUnnamedCount(UnnamedCountNameLength);
    }

    private void ReadQuestLog()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("ID_PLAYER");
            reader.ReadInt("SEQ_NUM");
            reader.ReadInt("CUR_TURN");
            reader.ReadInt("TYPE");
            reader.ReadString("ID_EVENT");
            reader.ReadInt("EFF_NUM");
        }
    }

    private void ReadTurnSummary()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("ID_PLAYER");
            int type = reader.ReadInt("TYPE");
            reader.ReadPosition();

            switch (type)
            {
                case 0:
                    reader.ReadString("ID_PLAYER2");
                    reader.ReadString("ID_SPELL");
                    reader.ReadString("ID_STK_D");
                    reader.ReadString("STR_STK_D");
                    break;
                case 1:
                    reader.ReadString("ID_PLAYER2");
                    reader.ReadString("ID_STK_A");
                    reader.ReadString("STR_STK_A");
                    reader.ReadString("ID_STK_D");
                    reader.ReadString("STR_STK_D");
                    break;
                case 2:
                case 5:
                    reader.ReadString("ID_STK_D");
                    reader.ReadString("STR_STK_D");
                    break;
                case 3:
                    reader.ReadString("ID_CITY");
                    break;
                case 4:
                    break;
                case 6:
                    reader.ReadString("ID_STK_D");
                    reader.ReadString("STR_STK_D");
                    reader.ReadBool("ACQUIRE");
                    break;
                default:
                    throw new InvalidDataException($"Unknown turn summary entry type '{type}' at offset {reader.Position}.");
            }
        }
    }

    private void ReadStackDestroyed()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("ID_STACK");
            reader.TryReadString("ID_KILLER");
            reader.TryReadString("SRCTMPL_ID");
        }
    }

    private void ReadRod()
    {
        Db.Id rodId = reader.ReadId("ROD_ID");
        Db.Id owner = reader.ReadId("OWNER");
        (int positionX, int positionY) = reader.ReadPosition();

        rods.Add(new Db.ScenarioRod(scenarioId, rodId, owner, positionX, positionY));
    }

    private void ReadTomb()
    {
        reader.ReadString("TOMB_ID");
        reader.ReadPosition();
        int count = reader.ReadInt("QTY_EP");

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("STACK_OWNR");
            reader.ReadString("KILLER");
            reader.ReadInt("TURN");
            reader.ReadString("STACK_NAME");
        }
    }

    private void ReadEvent()
    {
        Db.Id eventId = reader.ReadId("ID");
        AddText(eventId, Db.TextScenario.NameTextId, 0, reader.ReadString("NAME_TXT"));
        bool human = reader.TryReadBool("HUMAN") ?? false;
        bool dwarf = reader.TryReadBool("DWARF") ?? false;
        bool undead = reader.TryReadBool("UNDEAD") ?? false;
        bool heretic = reader.TryReadBool("HERETIC") ?? false;
        bool neutral = reader.TryReadBool("NEUTRAL") ?? false;
        bool? elf = reader.TryReadBool("ELF");
        bool verifiedHuman = reader.TryReadBool("VERHUMAN") ?? false;
        bool verifiedDwarf = reader.TryReadBool("VERDWARF") ?? false;
        bool verifiedUndead = reader.TryReadBool("VERUNDEAD") ?? false;
        bool verifiedHeretic = reader.TryReadBool("VERHERETIC") ?? false;
        bool verifiedNeutral = reader.TryReadBool("VERNEUTRAL") ?? false;
        bool? verifiedElf = reader.TryReadBool("VERELF");
        bool enabled = reader.ReadBool("ENABLED");
        bool occurOnce = reader.ReadBool("OCCUR_ONCE");
        int chance = reader.ReadInt("CHANCE");
        int order = reader.TryReadInt("ORDER") ?? 0;
        int conditionCount = reader.ReadInt("COND_QTY");

        for (int i = 0; i < conditionCount; i++)
        {
            ReadEventCondition(eventId);
        }

        int effectCount = reader.ReadInt("EFFECT_QTY");

        for (int i = 0; i < effectCount; i++)
        {
            ReadEventEffect(eventId);
        }

        events.Add(new Db.ScenarioEvent(
            scenarioId,
            eventId.Value,
            human,
            dwarf,
            undead,
            heretic,
            neutral,
            elf,
            enabled,
            occurOnce,
            chance,
            conditionCount,
            effectCount,
            verifiedHuman,
            verifiedDwarf,
            verifiedUndead,
            verifiedHeretic,
            verifiedNeutral,
            verifiedElf,
            order));
    }

    private void ReadEventCondition(Db.Id eventId)
    {
        Db.ScenarioEventConditionType category = (Db.ScenarioEventConditionType)reader.ReadInt("CATEGORY");
        Db.ScenarioEventCondition condition = CreateCondition(eventId, category);

        switch (category)
        {
            case Db.ScenarioEventConditionType.Frequency:
                condition = condition with { Frequency = reader.ReadInt("FREQUENCY") };
                break;
            case Db.ScenarioEventConditionType.EnteringZone:
                condition = condition with { IdLocation = reader.ReadOptionalId("ID_LOC") };
                break;
            case Db.ScenarioEventConditionType.EnteringCity:
            case Db.ScenarioEventConditionType.OwningCity:
                condition = condition with { IdCity = reader.ReadOptionalId("ID_CITY") };
                break;
            case Db.ScenarioEventConditionType.DestroyingStack:
                condition = condition with { IdStack = reader.ReadOptionalId("ID_STACK") };
                break;
            case Db.ScenarioEventConditionType.OwningItem:
                condition = condition with { TypeItem = reader.ReadOptionalId("TYPE_ITEM") };
                break;
            case Db.ScenarioEventConditionType.LeaderOwningItem:
                condition = condition with
                {
                    TypeItem = reader.ReadOptionalId("TYPE_ITEM"),
                    IdStack = reader.ReadOptionalId("ID_STACK")
                };
                break;
            case Db.ScenarioEventConditionType.DiplomacyRelations:
                condition = condition with
                {
                    IdPlayer1 = reader.ReadOptionalId("ID_PLAYER1"),
                    IdPlayer2 = reader.ReadOptionalId("ID_PLAYER2"),
                    Diplomacy = reader.ReadInt("DIPLOMACY")
                };
                break;
            case Db.ScenarioEventConditionType.Alliance:
                condition = condition with
                {
                    IdPlayer1 = reader.ReadOptionalId("ID_PLAYER1"),
                    IdPlayer2 = reader.ReadOptionalId("ID_PLAYER2")
                };
                break;
            case Db.ScenarioEventConditionType.LootingRuin:
                condition = condition with { IdRuin = reader.ReadOptionalId("ID_RUIN") };
                break;
            case Db.ScenarioEventConditionType.TransformingLand:
                condition = condition with { PercentLand = reader.ReadInt("PCT_LAND") };
                break;
            case Db.ScenarioEventConditionType.VisitingSite:
                condition = condition with { IdSite = reader.ReadOptionalId("ID_SITE") };
                break;
            case Db.ScenarioEventConditionType.StackToLocation:
                condition = condition with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    IdLocation = reader.ReadOptionalId("ID_LOC")
                };
                break;
            case Db.ScenarioEventConditionType.StackToCity:
                condition = condition with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    IdCity = reader.ReadOptionalId("ID_CITY")
                };
                break;
            case Db.ScenarioEventConditionType.ItemToLocation:
                condition = condition with
                {
                    TypeItem = reader.ReadOptionalId("TYPE_ITEM"),
                    IdLocation = reader.ReadOptionalId("ID_LOC")
                };
                break;
            case Db.ScenarioEventConditionType.StackExistence:
                condition = condition with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    MiscInt = reader.ReadInt("MISC_INT")
                };
                break;
            case Db.ScenarioEventConditionType.VariableInRange:
                condition = condition with
                {
                    MiscInt = reader.ReadInt("MISC_INT"),
                    MiscInt2 = reader.ReadInt("MISC_INT2"),
                    MiscInt3 = reader.ReadInt("MISC_INT3"),
                    MiscInt4 = reader.ReadInt("MISC_INT4"),
                    MiscInt5 = reader.ReadInt("MISC_INT5"),
                    MiscInt6 = reader.ReadInt("MISC_INT6"),
                    MiscInt7 = reader.ReadInt("MISC_INT7")
                };
                break;
            default:
                throw new InvalidDataException($"Unknown event condition category '{(int)category}' at offset {reader.Position}.");
        }

        eventConditions.Add(condition);
    }

    private Db.ScenarioEventCondition CreateCondition(Db.Id eventId, Db.ScenarioEventConditionType category)
    {
        return new Db.ScenarioEventCondition(
            scenarioId,
            eventId,
            null,
            category,
            null, null, null, null, null, null, null, null,
            null, null, null, null,
            null, null, null, null, null, null, null);
    }

    private Db.ScenarioEventEffect CreateEffect(Db.Id eventId, int number, Db.ScenarioEventEffectType category)
    {
        return new Db.ScenarioEventEffect(
            scenarioId,
            eventId,
            number,
            category,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null);
    }

    private void ReadEventEffect(Db.Id eventId)
    {
        Db.ScenarioEventEffectType category = (Db.ScenarioEventEffectType)reader.ReadInt("CATEGORY");
        int number = reader.ReadInt("NUM");
        Db.ScenarioEventEffect effect = CreateEffect(eventId, number, category);

        switch (category)
        {
            case Db.ScenarioEventEffectType.Win:
                effect = effect with
                {
                    WinScenario = reader.ReadBool("WIN_SCEN"),
                    IdPlayer1 = reader.TryReadOptionalId("ID_PLAYER1")
                };
                break;
            case Db.ScenarioEventEffectType.CreateStack:
                effect = effect with
                {
                    IdStackTemplate = reader.ReadOptionalId("ID_STKTEMP"),
                    IdLocation = reader.ReadOptionalId("ID_LOC")
                };
                break;
            case Db.ScenarioEventEffectType.CastSpell:
                effect = effect with
                {
                    TypeSpell = reader.ReadOptionalId("TYPE_SPELL"),
                    IdPlayer1 = reader.TryReadOptionalId("ID_PLAYER1")
                };
                break;
            case Db.ScenarioEventEffectType.CastSpellMap:
                effect = effect with
                {
                    TypeSpell = reader.ReadOptionalId("TYPE_SPELL"),
                    IdLocation = reader.ReadOptionalId("ID_LOC"),
                    IdPlayer1 = reader.TryReadOptionalId("ID_PLAYER1")
                };
                break;
            case Db.ScenarioEventEffectType.ChangeOwner:
                effect = effect with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    IdPlayer1 = reader.ReadOptionalId("ID_PLAYER1"),
                    FirstOnly = reader.TryReadBool("FIRST_ONLY"),
                    PlayAnim = reader.TryReadBool("PLAY_ANIM")
                };
                break;
            case Db.ScenarioEventEffectType.MoveBeside:
                effect = effect with { IdStack = reader.ReadOptionalId("ID_STACK") };
                break;
            case Db.ScenarioEventEffectType.Battle:
                effect = effect with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    FirstOnly = reader.TryReadBool("FIRST_ONLY")
                };
                break;
            case Db.ScenarioEventEffectType.EnableEvent:
                effect = effect with
                {
                    IdEvent = reader.ReadOptionalId("ID_EVENT"),
                    Enable = reader.ReadBool("ENABLE")
                };
                break;
            case Db.ScenarioEventEffectType.GiveSpell:
                effect = effect with { TypeSpell = reader.ReadOptionalId("TYPE_SPELL") };
                break;
            case Db.ScenarioEventEffectType.GiveItem:
                effect = effect with
                {
                    GiveTo = reader.TryReadInt("GIVETO"),
                    TypeItem = reader.ReadOptionalId("TYPE_ITEM")
                };
                break;
            case Db.ScenarioEventEffectType.MoveStack:
                effect = effect with
                {
                    IdStackTemplate = reader.ReadOptionalId("ID_STKTEMP"),
                    IdLocation = reader.ReadOptionalId("ID_LOC"),
                    BoolValue = reader.TryReadBool("BOOLVALUE")
                };
                break;
            case Db.ScenarioEventEffectType.Ally:
                effect = effect with
                {
                    IdPlayer1 = reader.ReadOptionalId("ID_PLAYER1"),
                    IdPlayer2 = reader.ReadOptionalId("ID_PLAYER2")
                };

                if (format.HasDesignData)
                {
                    effect = effect with { PermanentAlliance = reader.ReadBool("PERMALLI") };
                }

                break;
            case Db.ScenarioEventEffectType.Diplomacy:
                effect = effect with
                {
                    IdPlayer1 = reader.ReadOptionalId("ID_PLAYER1"),
                    IdPlayer2 = reader.ReadOptionalId("ID_PLAYER2"),
                    Diplomacy = reader.ReadInt("DIPLOMACY"),
                    Enable = reader.TryReadBool("ENABLE")
                };
                break;
            case Db.ScenarioEventEffectType.Unfog:
                effect = effect with { IdLocation = reader.ReadOptionalId("ID_LOC") };
                int audienceCount = reader.ReadUnnamedCount(UnnamedCountNameLength);

                for (int i = 0; i < audienceCount; i++)
                {
                    Db.Id audienceEventId = reader.ReadId("EVENT_ID");
                    Db.Id audiencePlayerId = reader.ReadId("PLAYER");

                    eventUnfogs.Add(new Db.ScenarioEventUnfog(scenarioId, audienceEventId, audiencePlayerId));
                }

                if (format.HasDesignData)
                {
                    effect = effect with
                    {
                        Enable = reader.ReadBool("ENABLE"),
                        NumValue = reader.TryReadInt("NUMVALUE")
                    };
                }

                break;
            case Db.ScenarioEventEffectType.RemoveMountain:
                effect = effect with { IdLocation = reader.ReadOptionalId("ID_LOC") };
                break;
            case Db.ScenarioEventEffectType.RemoveLandmark:
                effect = effect with
                {
                    IdLandmark = reader.ReadOptionalId("ID_LMARK"),
                    BoolValue = reader.TryReadBool("BOOLVALUE")
                };
                break;
            case Db.ScenarioEventEffectType.ChangeObjective:
                AddText(eventId, Db.TextScenario.ObjectiveTextId, 0, reader.ReadString("OBJECT_TXT"));
                break;
            case Db.ScenarioEventEffectType.Popup:
                AddText(eventId, Db.TextScenario.PopupTextId, number, reader.ReadString("POPUP_TXT"));
                effect = effect with
                {
                    Music = reader.ReadOptionalString("MUSIC"),
                    Sound = reader.ReadOptionalString("SOUND"),
                    Image = reader.ReadOptionalString("IMAGE"),
                    Image2 = reader.ReadOptionalString("IMAGE2"),
                    LeftSide = reader.TryReadBool("LEFT_SIDE"),
                    PopupShow = reader.TryReadString("POPUP_SHOW") ?? "TRI",
                    BoolValue = reader.TryReadBool("BOOLVALUE")
                };
                break;
            case Db.ScenarioEventEffectType.ChangeOrder:
                effect = effect with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    OrderTarget = reader.ReadOptionalId("ORDER_TARG"),
                    FirstOnly = reader.TryReadBool("FIRST_ONLY"),
                    Order = (Db.OrderType)reader.ReadInt("ORDER")
                };
                break;
            case Db.ScenarioEventEffectType.DestroyItem:
                effect = effect with
                {
                    TypeItem = reader.ReadOptionalId("TYPE_ITEM"),
                    TriggerOnly = reader.ReadBool("TRIG_ONLY")
                };
                break;
            case Db.ScenarioEventEffectType.RemoveStack:
                effect = effect with
                {
                    IdStack = reader.ReadOptionalId("ID_STACK"),
                    FirstOnly = reader.ReadBool("FIRST_ONLY")
                };
                break;
            case Db.ScenarioEventEffectType.ChangeLandmark:
                effect = effect with
                {
                    IdLandmark = reader.ReadOptionalId("ID_LMARK"),
                    TypeLandmark = reader.ReadOptionalId("TYPE_LMARK")
                };
                break;
            case Db.ScenarioEventEffectType.ChangeTerrain:
                effect = effect with
                {
                    IdLocation = reader.ReadOptionalId("ID_LOC"),
                    Lookup = reader.ReadInt("LOOKUP"),
                    NumValue = reader.ReadInt("NUMVALUE")
                };
                break;
            case Db.ScenarioEventEffectType.ModifyVariable:
                effect = effect with
                {
                    Lookup = reader.ReadInt("LOOKUP"),
                    NumValue = reader.ReadInt("NUMVALUE"),
                    NumValue2 = reader.ReadInt("NUMVALUE2")
                };
                break;
            default:
                throw new InvalidDataException($"Unknown event effect category '{(int)category}' at offset {reader.Position}.");
        }

        eventEffects.Add(effect);
    }

    private CityData ReadCity()
    {
        Db.Id cityId = reader.ReadId("CITY_ID");
        AddText(cityId, Db.TextScenario.NameTextId, 0, reader.ReadString("NAME_TXT"));
        AddText(cityId, Db.TextScenario.DescriptionTextId, 0, reader.ReadString("DESC_TXT"));
        Db.Id owner = reader.ReadId("OWNER");
        Db.Id subrace = ReadSubraceId(cityId);
        Db.Id? stack = reader.ReadOptionalId("STACK");
        (int positionX, int positionY) = reader.ReadPosition();

        CheckUnitGroupOwner(ReadUnitGroup(), cityId);

        return new CityData(cityId, owner, subrace, stack, positionX, positionY);
    }

    private Db.ScenarioFortification CreateFortification(CityData city, int aiPriority)
    {
        return new Db.ScenarioFortification(
            scenarioId,
            city.CityId,
            city.PositionX,
            city.PositionY,
            city.Owner,
            city.Subrace,
            city.Stack,
            null, null, null, null, null, null,
            null, null, null, null, null,
            aiPriority,
            null);
    }

    private SiteData ReadSite()
    {
        Db.Id siteId = reader.ReadId("SITE_ID");
        int imageIso = reader.ReadInt("IMG_ISO");
        string? imageInterface = reader.ReadOptionalString("IMG_INTF");
        AddText(siteId, Db.TextScenario.SiteTitleTextId, 0, reader.ReadString("TXT_TITLE"));
        AddText(siteId, Db.TextScenario.SiteDescriptionTextId, 0, reader.ReadString("TXT_DESC"));
        (int positionX, int positionY) = reader.ReadPosition();
        Db.Id? visiter = reader.ReadOptionalId("VISITER");
        int aiPriority = reader.ReadInt("AIPRIORITY");

        return new SiteData(siteId, imageIso, imageInterface, visiter, positionX, positionY, aiPriority);
    }

    private Db.ScenarioSite CreateSite(SiteData site)
    {
        return new Db.ScenarioSite(
            scenarioId,
            site.SiteId,
            site.ImageIso,
            site.ImageInterface,
            site.VisiterId,
            site.PositionX,
            site.PositionY,
            null, null, null, null,
            null, null, null, null, null, null, null, null, null,
            site.AiPriority);
    }

    private void ReadSiteVisitors()
    {
        int count = reader.ReadUnnamedCount(UnnamedCountNameLength);

        for (int i = 0; i < count; i++)
        {
            reader.ReadString("SITE_ID");
            reader.ReadString("VISITER");
        }
    }

    private Db.Id ReadUnitGroup()
    {
        Db.Id groupId = reader.ReadId("GROUP_ID");

        Db.Id?[] groupUnits = new Db.Id?[6];
        for (int i = 0; i < groupUnits.Length; i++)
        {
            groupUnits[i] = reader.ReadOptionalId($"UNIT_{i}");
        }

        int[] positions = new int[6];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = reader.ReadInt($"POS_{i}");
        }

        unitGroups.Add(new Db.ScenarioUnitGroup(
            scenarioId,
            groupId,
            groupUnits[0],
            groupUnits[1],
            groupUnits[2],
            groupUnits[3],
            groupUnits[4],
            groupUnits[5],
            positions[0],
            positions[1],
            positions[2],
            positions[3],
            positions[4],
            positions[5]));

        return groupId;
    }

    private static void CheckUnitGroupOwner(Db.Id groupId, Db.Id ownerId)
    {
        if (groupId != ownerId)
        {
            throw new InvalidDataException($"Unit group '{groupId}' does not belong to '{ownerId}'.");
        }
    }

    private void AddInventory(Db.Id containerId, IReadOnlyList<Db.Id> containerItems)
    {
        foreach (Db.Id itemId in containerItems)
        {
            inventories.Add(new Db.ScenarioInventory(scenarioId, containerId, itemId));
        }
    }

    private void AddText(Db.Id objectId, string stringId, int sequenceNumber, string text)
    {
        if (text.Length > 0)
        {
            texts.Add(new Db.TextScenario(scenarioId, objectId, stringId, text, sequenceNumber));
        }
    }

    private Db.Id ReadSubraceId(Db.Id ownerId)
    {
        Db.Id? subrace = reader.TryReadId("SUBRACE");

        if (subrace is null)
        {
            throw new InvalidDataException($"Scenario object '{ownerId}' has no subrace.");
        }

        return subrace.Value;
    }

    private static Db.ResourceSet GetDefaultSpellBank(Db.ResourceSet bank, bool isHuman)
    {
        if (isHuman)
        {
            return Db.ResourceSet.Empty;
        }
        else
        {
            return bank;
        }
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return 0;
    }
}
