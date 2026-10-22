using System.Globalization;

namespace Gallean.Engine;

internal static class ScenarioTextIdExtensions
{
    extension(ScenarioTextId)
    {
        public static ScenarioTextId MakeInternal(Db.Id idObject, string idString, int sequenceNumber)
        {
            return new ScenarioTextId(string.Create(CultureInfo.InvariantCulture, $"{idObject}:{idString}:{sequenceNumber}"));
        }
    }
}
