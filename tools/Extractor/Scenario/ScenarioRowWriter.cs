using System.Text.Json;

namespace Extractor;

internal static class ScenarioRowWriter
{
    public static void Write(Utf8JsonWriter writer, Db.CampaignScenario row)
    {
        writer.WriteStartObject();
        writer.WriteString("CAMP_ID", row.CampaignId.Value);
        WriteNumber(writer, "SCEN_NO", row.ScenarioNumber);
        writer.WriteString("SCEN_ID", row.ScenarioId.Value);
        writer.WriteString("SMK_BRIEF", row.BriefingVideo);
        writer.WriteString("SMK_WIN", row.WinVideo);
        writer.WriteString("SMK_LOSE", row.LoseVideo);
        writer.WriteString("WAV_WIN", row.WinAudio);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioBag row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("BAG_ID", row.BagId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("OBJV", row.ObjValue);
        writer.WriteNumber("IMAGE", row.Image);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioCrystal row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("CRYSTAL_ID", row.CrystalId.Value);
        writer.WriteNumber("RESOURCE", row.Resource);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioEvent row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID", row.Id);
        writer.WriteBoolean("HUMAN", row.Human);
        writer.WriteBoolean("DWARF", row.Dwarf);
        writer.WriteBoolean("UNDEAD", row.Undead);
        writer.WriteBoolean("HERETIC", row.Heretic);
        writer.WriteBoolean("NEUTRAL", row.Neutral);
        WriteBoolean(writer, "ELF", row.Elf);
        writer.WriteBoolean("ENABLED", row.Enabled);
        writer.WriteBoolean("OCCUR_ONCE", row.OccurOnce);
        writer.WriteNumber("CHANCE", row.Chance);
        writer.WriteNumber("COND_QTY", row.CondQuantity);
        writer.WriteNumber("EFFECT_QTY", row.EffectQuantity);
        writer.WriteBoolean("VERHUMAN", row.VerifiedHuman);
        writer.WriteBoolean("VERDWARF", row.VerifiedDwarf);
        writer.WriteBoolean("VERUNDEAD", row.VerifiedUndead);
        writer.WriteBoolean("VERHERETIC", row.VerifiedHeretic);
        writer.WriteBoolean("VERNEUTRAL", row.VerifiedNeutral);
        WriteBoolean(writer, "VERELF", row.VerifiedElf);
        writer.WriteNumber("ORDER", row.Order);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioEventCondition row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID", row.Id.Value);
        writer.WriteString("NUM", row.Number);
        writer.WriteNumber("CATEGORY", (int)row.Category);
        WriteId(writer, "ID_LOC", row.IdLocation);
        WriteId(writer, "ID_STACK", row.IdStack);
        WriteId(writer, "ID_RUIN", row.IdRuin);
        WriteId(writer, "ID_CITY", row.IdCity);
        WriteId(writer, "ID_SITE", row.IdSite);
        WriteId(writer, "ID_PLAYER1", row.IdPlayer1);
        WriteId(writer, "ID_PLAYER2", row.IdPlayer2);
        WriteId(writer, "TYPE_ITEM", row.TypeItem);
        WriteNumber(writer, "FREQUENCY", row.Frequency);
        WriteNumber(writer, "PCT_LAND", row.PercentLand);
        WriteNumber(writer, "DIPLOMACY", row.Diplomacy);
        writer.WriteString("ALLIANCE", row.Alliance);
        WriteNumber(writer, "MISC_INT", row.MiscInt);
        WriteNumber(writer, "MISC_INT2", row.MiscInt2);
        WriteNumber(writer, "MISC_INT3", row.MiscInt3);
        WriteNumber(writer, "MISC_INT4", row.MiscInt4);
        WriteNumber(writer, "MISC_INT5", row.MiscInt5);
        WriteNumber(writer, "MISC_INT6", row.MiscInt6);
        WriteNumber(writer, "MISC_INT7", row.MiscInt7);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioEventEffect row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID", row.Id.Value);
        writer.WriteNumber("NUM", row.Number);
        writer.WriteNumber("CATEGORY", (int)row.Category);
        WriteId(writer, "ID_LOC", row.IdLocation);
        WriteId(writer, "ID_STKTEMP", row.IdStackTemplate);
        WriteId(writer, "TYPE_SPELL", row.TypeSpell);
        writer.WriteString("SOUND", row.Sound);
        writer.WriteString("MUSIC", row.Music);
        writer.WriteString("IMAGE", row.Image);
        writer.WriteString("IMAGE2", row.Image2);
        WriteBoolean(writer, "LEFT_SIDE", row.LeftSide);
        WriteId(writer, "ID_STACK", row.IdStack);
        WriteBoolean(writer, "FIRST_ONLY", row.FirstOnly);
        WriteId(writer, "ID_PLAYER1", row.IdPlayer1);
        WriteId(writer, "ID_EVENT", row.IdEvent);
        WriteId(writer, "TYPE_ITEM", row.TypeItem);
        WriteNumber(writer, "GIVETO", row.GiveTo);
        WriteId(writer, "ID_PLAYER2", row.IdPlayer2);
        WriteNumber(writer, "DIPLOMACY", row.Diplomacy);
        WriteId(writer, "ID_LMARK", row.IdLandmark);
        WriteId(writer, "TYPE_LMARK", row.TypeLandmark);
        WriteBoolean(writer, "ENABLE", row.Enable);
        WriteNumber(writer, "ORDER", (int?)row.Order);
        WriteId(writer, "ORDER_TARG", row.OrderTarget);
        WriteBoolean(writer, "TRIG_ONLY", row.TriggerOnly);
        WriteBoolean(writer, "WIN_SCEN", row.WinScenario);
        WriteBoolean(writer, "PLAY_ANIM", row.PlayAnim);
        writer.WriteString("POPUP_SHOW", row.PopupShow);
        WriteBoolean(writer, "PERMALLI", row.PermanentAlliance);
        WriteNumber(writer, "LOOKUP", row.Lookup);
        WriteNumber(writer, "NUMVALUE", row.NumValue);
        WriteBoolean(writer, "BOOLVALUE", row.BoolValue);
        WriteNumber(writer, "NUMVALUE2", row.NumValue2);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioEventUnfog row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("EVENT_ID", row.EventId.Value);
        writer.WriteString("PLAYER", row.Player.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioFog row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("FOG_ID", row.FogId.Value);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("FOG", row.Fog);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioFortification row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("CITY_ID", row.CityId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("OWNER", row.OwnerId.Value);
        writer.WriteString("SUBRACE", row.SubraceId.Value);
        WriteId(writer, "STACK", row.Stack);
        WriteNumber(writer, "SIZE", row.Size);
        writer.WriteString("SIZE_MAX", row.SizeMax);
        WriteNumber(writer, "REGEN_B", row.RegenerationBonus);
        WriteId(writer, "PROTECT_B", row.ProtectionBuildingId);
        WriteNumber(writer, "MORALE", row.Morale);
        WriteNumber(writer, "GROWTH_T", row.GrowthTurn);
        WriteBoolean(writer, "P_O_UN", row.ProtectedFromUndead);
        WriteBoolean(writer, "P_O_HE", row.ProtectedFromHeretic);
        WriteBoolean(writer, "P_O_HU", row.ProtectedFromHuman);
        WriteBoolean(writer, "P_O_DW", row.ProtectedFromDwarf);
        WriteBoolean(writer, "P_O_EL", row.ProtectedFromElf);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        WriteNumber(writer, "RIOT_T", row.RiotTurns);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioInfo row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("INFO_ID", row.InfoId.Value);
        writer.WriteNumber("CUR_TURN", row.CurrentTurn);
        writer.WriteNumber("MAP_SEED", row.MapSeed);
        writer.WriteNumber("MAP_SIZE", row.MapSize);
        writer.WriteNumber("QTY_CITIES", row.QuantityCities);
        writer.WriteString("CREATOR", row.Creator);
        writer.WriteBoolean("SOURCE_M", row.SourceM);
        writer.WriteNumber("DIFFGAME", (int)row.DifficultyGame);
        writer.WriteNumber("DIFFSCEN", (int)row.DifficultyScenario);
        writer.WriteString("CAMPAIGN", row.CampaignId.Value);
        writer.WriteNumber("MAX_UNIT", row.MaxUnit);
        writer.WriteNumber("MAX_LEADER", row.MaxLeader);
        writer.WriteNumber("MAX_CITY", row.MaxCity);
        writer.WriteNumber("MAX_SPELL", row.MaxSpell);
        writer.WriteNumber("PLAYER_1", row.Player1);
        writer.WriteNumber("PLAYER_2", row.Player2);
        writer.WriteNumber("PLAYER_3", row.Player3);
        writer.WriteNumber("PLAYER_4", row.Player4);
        writer.WriteNumber("PLAYER_5", row.Player5);
        writer.WriteNumber("PLAYER_6", row.Player6);
        writer.WriteNumber("PLAYER_7", row.Player7);
        writer.WriteNumber("PLAYER_8", row.Player8);
        writer.WriteNumber("PLAYER_9", row.Player9);
        writer.WriteNumber("PLAYER_10", row.Player10);
        writer.WriteNumber("PLAYER_11", row.Player11);
        writer.WriteNumber("PLAYER_12", row.Player12);
        writer.WriteNumber("PLAYER_13", row.Player13);
        writer.WriteString("NAME", row.Name);
        writer.WriteString("DESC", row.Description);
        writer.WriteString("BRIEFING", row.Briefing);
        writer.WriteString("BRIEFLONG1", row.BriefingLong1);
        writer.WriteString("BRIEFLONG2", row.BriefingLong2);
        writer.WriteString("BRIEFLONG3", row.BriefingLong3);
        writer.WriteString("BRIEFLONG4", row.BriefingLong4);
        writer.WriteString("BRIEFLONG5", row.BriefingLong5);
        writer.WriteString("DEBUNKW", row.DebunkWin);
        writer.WriteString("DEBUNKW2", row.DebunkWin2);
        writer.WriteString("DEBUNKW3", row.DebunkWin3);
        writer.WriteString("DEBUNKW4", row.DebunkWin4);
        writer.WriteString("DEBUNKW5", row.DebunkWin5);
        writer.WriteString("DEBUNKL", row.DebunkLose);
        writer.WriteNumber("SUGG_LVL", row.SuggestedLevel);
        writer.WriteBoolean("O", row.O);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioInventory row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("INV_ID", row.InventoryId.Value);
        writer.WriteString("ITEM_ID", row.ItemId.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioItem row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ITEM_ID", row.ItemId.Value);
        writer.WriteString("ITEM_TYPE", row.ItemType.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioLandmark row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("LMARK_ID", row.LandmarkId.Value);
        writer.WriteString("TYPE", row.TypeId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("DESC_TXT", row.DescriptionText);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioLocation row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("LOC_ID", row.LocationId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteNumber("RADIUS", row.Radius);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMage row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("SITE_ID", row.SiteId.Value);
        writer.WriteString("SPELL_ID", row.SpellId.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMapBlock row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("BLOCKID", row.BlockId.Value);
        writer.WriteString("BLOCKDATA", row.BlockData);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMapElement row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("ELEMENT", row.ElementId.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMercenary row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("SITE_ID", row.SiteId.Value);
        writer.WriteString("UNIT_ID", row.UnitId.Value);
        writer.WriteNumber("UNIT_LEVEL", row.UnitLevel);
        writer.WriteBoolean("UNIT_UNIQ", row.UnitUniq);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMerchant row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("SITE_ID", row.SiteId.Value);
        writer.WriteString("ITEM_ID", row.ItemId.Value);
        writer.WriteNumber("ITEM_COUNT", row.ItemCount);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioMountain row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteNumber("ID_MOUNT", row.IdMountain);
        writer.WriteNumber("SIZE_X", row.SizeX);
        writer.WriteNumber("SIZE_Y", row.SizeY);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteNumber("IMAGE", row.Image);
        writer.WriteNumber("RACE", row.Race);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioObjectMap row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("OBJ_ID", row.ObjId.Value);
        writer.WriteString("WHAT", row.What);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioPlayer row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("PLAYER_ID", row.PlayerId.Value);
        writer.WriteString("RACE_ID", row.RaceId.Value);
        writer.WriteString("LORD_ID", row.LordId.Value);
        writer.WriteString("FOG_ID", row.FogId.Value);
        writer.WriteNumber("RESEAR_T", row.ResearchTurn);
        writer.WriteNumber("CONSTR_T", row.ConstructTurn);
        writer.WriteString("BUILDS_ID", row.BuildsId.Value);
        writer.WriteString("KNOWN_ID", row.KnownId.Value);
        writer.WriteString("BANK", row.Bank.Value);
        writer.WriteString("SPELL_BANK", row.SpellBank.Value);
        writer.WriteNumber("ATTITUDE", (int)row.Attitude);
        writer.WriteNumber("FACE", row.Face);
        WriteId(writer, "SPY_1", row.Spy1Id);
        WriteId(writer, "SPY_2", row.Spy2Id);
        WriteId(writer, "SPY_3", row.Spy3Id);
        writer.WriteNumber("QTY_BREAKS", row.QuantityBreaks);
        writer.WriteBoolean("IS_HUMAN", row.IsHuman);
        WriteId(writer, "CAPT_BY", row.CapturedById);
        WriteBoolean(writer, "ALWAYSAI", row.AlwaysAi);
        WriteId(writer, "EXMAPID1", row.ExMapId1);
        WriteId(writer, "EXMAPID2", row.ExMapId2);
        WriteId(writer, "EXMAPID3", row.ExMapId3);
        writer.WriteNumber("EXMAPTURN1", row.ExMapTurn1);
        writer.WriteNumber("EXMAPTURN2", row.ExMapTurn2);
        writer.WriteNumber("EXMAPTURN3", row.ExMapTurn3);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioRelation row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteNumber("RACE_1", (int)row.Race1);
        writer.WriteNumber("RACE_2", (int)row.Race2);
        writer.WriteNumber("RELATION", row.Relation);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioRoad row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ROAD_ID", row.RoadId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteNumber("INDEX", row.Index);
        writer.WriteNumber("VAR", row.Var);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioRod row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ROD_ID", row.RodId.Value);
        writer.WriteString("OWNER", row.OwnerId.Value);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioRuin row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("RUIN_ID", row.RuinId.Value);
        writer.WriteNumber("IMAGE", row.Image);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("CASH", row.Cash);
        WriteId(writer, "ITEM", row.Item);
        WriteId(writer, "LOOTER", row.Looter);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioSite row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("SITE_ID", row.SiteId.Value);
        writer.WriteNumber("IMG_ISO", row.ImageIso);
        writer.WriteString("IMG_INTF", row.ImageInterface);
        WriteId(writer, "VISITER", row.VisiterId);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("QTY_VISIT", row.QuantityVisit);
        WriteNumber(writer, "QTY_SPELL", row.QuantitySpell);
        WriteNumber(writer, "QTY_UNIT", row.QuantityUnit);
        WriteNumber(writer, "QTY_ITEM", row.QuantityItem);
        WriteBoolean(writer, "BUY_ARMOR", row.BuyArmor);
        WriteBoolean(writer, "BUY_JEWEL", row.BuyJewel);
        WriteBoolean(writer, "BUY_WEAPON", row.BuyWeapon);
        WriteBoolean(writer, "BUY_BANNER", row.BuyBanner);
        WriteBoolean(writer, "BUY_POTION", row.BuyPotion);
        WriteBoolean(writer, "BUY_SCROLL", row.BuyScroll);
        WriteBoolean(writer, "BUY_WAND", row.BuyWand);
        WriteBoolean(writer, "BUY_VALUE", row.BuyValue);
        WriteBoolean(writer, "MISSION", row.Mission);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioSpell row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("KSPELLS_ID", row.KnownSpellsId.Value);
        writer.WriteString("SPELL_ID", row.SpellId.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioStack row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("STACK_ID", row.StackId.Value);
        WriteId(writer, "SRCTMPL_ID", row.SourceTemplateId);
        writer.WriteNumber("POS_X", row.PositionX);
        writer.WriteNumber("POS_Y", row.PositionY);
        writer.WriteString("LEADER_ID", row.LeaderId.Value);
        writer.WriteBoolean("LEADR_ALIV", row.LeaderAlive);
        writer.WriteNumber("MORALE", row.Morale);
        writer.WriteNumber("MOVE", row.Move);
        writer.WriteNumber("FACING", row.Facing);
        WriteId(writer, "BANNER", row.BannerId);
        WriteId(writer, "TOME", row.TomeId);
        WriteId(writer, "BATTLE1", row.Battle1Id);
        WriteId(writer, "BATTLE2", row.Battle2Id);
        WriteId(writer, "ARTIFACT1", row.Artifact1Id);
        WriteId(writer, "ARTIFACT2", row.Artifact2Id);
        WriteId(writer, "BOOTS", row.BootsId);
        writer.WriteString("OWNER", row.OwnerId.Value);
        writer.WriteString("SUBRACE", row.SubraceId.Value);
        WriteId(writer, "INSIDE", row.InsideId);
        writer.WriteBoolean("INVISIBLE", row.Invisible);
        writer.WriteNumber("UPGCOUNT", row.UpgradeCount);
        writer.WriteBoolean("AI_IGNORE", row.AiIgnore);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteNumber("ORDER", (int)row.Order);
        WriteId(writer, "ORDER_TARG", row.OrderTargetId);
        writer.WriteNumber("CREAT_LVL", row.CreationLevel);
        writer.WriteNumber("NBBATTLE", row.NumberOfBattles);
        writer.WriteNumber("AIORDER", (int)row.AiOrder);
        WriteId(writer, "AIORDERTAR", row.AiOrderTargetId);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioStackTemplate row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID", row.Id.Value);
        writer.WriteString("LEADER", row.Leader.Value);
        writer.WriteNumber("LEADER_LVL", row.LeaderLevel);
        writer.WriteString("OWNER", row.OwnerId.Value);
        writer.WriteString("SUBRACE", row.SubraceId.Value);
        writer.WriteString("UNIT_0", row.Unit0Id.Value);
        writer.WriteNumber("UNIT_0_LVL", row.Unit0Level);
        WriteId(writer, "UNIT_1", row.Unit1Id);
        writer.WriteNumber("UNIT_1_LVL", row.Unit1Level);
        WriteId(writer, "UNIT_2", row.Unit2Id);
        writer.WriteNumber("UNIT_2_LVL", row.Unit2Level);
        WriteId(writer, "UNIT_3", row.Unit3Id);
        writer.WriteNumber("UNIT_3_LVL", row.Unit3Level);
        WriteId(writer, "UNIT_4", row.Unit4Id);
        writer.WriteNumber("UNIT_4_LVL", row.Unit4Level);
        WriteId(writer, "UNIT_5", row.Unit5Id);
        writer.WriteNumber("UNIT_5_LVL", row.Unit5Level);
        writer.WriteNumber("POS_0", row.Position0);
        writer.WriteNumber("POS_1", row.Position1);
        writer.WriteNumber("POS_2", row.Position2);
        writer.WriteNumber("POS_3", row.Position3);
        writer.WriteNumber("POS_4", row.Position4);
        writer.WriteNumber("POS_5", row.Position5);
        writer.WriteNumber("ORDER", (int)row.Order);
        WriteId(writer, "ORDER_TARG", row.OrderTargetId);
        writer.WriteBoolean("USE_FACING", row.UseFacing);
        writer.WriteNumber("FACING", row.Facing);
        writer.WriteNumber("AIPRIORITY", row.AiPriority);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioStackTemplateModifier row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("TEMPLAT_ID", row.TemplateId.Value);
        writer.WriteString("MODIF_ID", row.ModifierId.Value);
        writer.WriteNumber("UNIT_POS", row.UnitPos);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioSubrace row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("SUBRACE_ID", row.SubraceId.Value);
        writer.WriteString("PLAYER_ID", row.PlayerId.Value);
        writer.WriteNumber("SUBRACE", row.Subrace);
        writer.WriteNumber("BANNER", row.Banner);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioTalismanCharges row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID_TALIS", row.IdTalisman.Value);
        writer.WriteNumber("CHARGES", row.Charges);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioUnit row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("UNIT_ID", row.UnitId.Value);
        writer.WriteNumber("HP", row.Hp);
        writer.WriteNumber("XP", row.Xp);
        writer.WriteNumber("CREATION", row.Creation);
        writer.WriteString("TYPE", row.TypeId.Value);
        writer.WriteNumber("LEVEL", row.Level);
        writer.WriteBoolean("TRANSF", row.Transf);
        WriteBoolean(writer, "DYNLEVEL", row.DynLevel);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioUnitGroup row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("GROUP_ID", row.GroupId.Value);
        WriteId(writer, "UNIT_0", row.Unit0Id);
        WriteId(writer, "UNIT_1", row.Unit1Id);
        WriteId(writer, "UNIT_2", row.Unit2Id);
        WriteId(writer, "UNIT_3", row.Unit3Id);
        WriteId(writer, "UNIT_4", row.Unit4Id);
        WriteId(writer, "UNIT_5", row.Unit5Id);
        writer.WriteNumber("POS_0", row.Pos0);
        writer.WriteNumber("POS_1", row.Pos1);
        writer.WriteNumber("POS_2", row.Pos2);
        writer.WriteNumber("POS_3", row.Pos3);
        writer.WriteNumber("POS_4", row.Pos4);
        writer.WriteNumber("POS_5", row.Pos5);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioUnitModifier row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("UNIT_ID", row.UnitId.Value);
        writer.WriteString("MODIF_ID", row.ModifierId.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.ScenarioVariable row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteNumber("ID", row.Id);
        writer.WriteString("NAME", row.Name);
        writer.WriteNumber("VALUE", row.Value);
        writer.WriteEndObject();
    }

    public static void Write(Utf8JsonWriter writer, Db.TextScenario row)
    {
        writer.WriteStartObject();
        writer.WriteString("NO_SCEN", row.ScenarioId.Value);
        writer.WriteString("ID_OBJECT", row.IdObject.Value);
        writer.WriteString("ID_STRING", row.IdString);
        writer.WriteString("TEXT", row.Text);
        writer.WriteNumber("SEQ_NUM", row.SeqNum);
        writer.WriteEndObject();
    }

    private static void WriteId(Utf8JsonWriter writer, string name, Db.Id? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value.Value.Value);
        }
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteBoolean(Utf8JsonWriter writer, string name, bool? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteBoolean(name, value.Value);
        }
    }
}
