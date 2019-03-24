using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Progopoly.Models
{
    public static class Constants
    {
        public static int GO_TILE_SALARY = 200;
        public static int MINIMUM_AUCTION_RAISE_BET = 10;
        public static int GET_OUT_OF_JAIL_FEE = 100;
        public static int SINGLE_RAILROAD_FEE = 25;
        public static int STARTING_MONEY = 1500;
        public static int MAX_BUILDINGS_ON_PROPERTY = 5;
        public static int DEFAULT_TURN_TIMEOUT_SECONDS = 25;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum PlayerColor
        {
            NoColor,
            Red,
            Green,
            Blue,
            Yellow,
            Cyan,
            Magenta
        };

        [JsonConverter(typeof(StringEnumConverter))]
        public enum PropertyColor
        {
            NoColor,
            Brown,
            LightBlue,
            Pink,
            Orange,
            Red,
            Yellow,
            Green,
            DarkBlue
        };

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TileType
        {
            Unknown,
            Go,
            ColorProperty,
            CommunityChest,
            Chance,
            Tax,
            Railroad,
            Utilities,
            Jail,
            FreeParking,
            GoToJail
        };

        public static ReadOnlyDictionary<PropertyColor, int> PropertyColorBuildingCost = new ReadOnlyDictionary<PropertyColor, int>(new Dictionary<PropertyColor, int>()
        {
            { PropertyColor.Brown, 50},
            { PropertyColor.LightBlue, 50},
            { PropertyColor.Pink, 100},
            { PropertyColor.Orange, 100},
            { PropertyColor.Red, 150},
            { PropertyColor.Yellow, 150},
            { PropertyColor.Green, 200},
            { PropertyColor.DarkBlue, 200}
        });
    }
}