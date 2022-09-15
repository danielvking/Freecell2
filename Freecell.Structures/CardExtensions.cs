using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Structures
{
    public static class CardExtensions
    {
        public static FaceValue FaceValue(this Card self)
        {
            return (FaceValue)((byte)self >> 2);
        }
        
        public static Suit? Suit(this Card self)
        {
            if ((int)self >> 2 == 0) return null;
            return (Suit)((byte)self & 0b11);
        }

        public static CardColor? Color(this Card self)
        {
            if ((int)self >> 2 == 0) return null;
            return (CardColor)((byte)self & 1);
        }

        public static Card Create(FaceValue faceValue, Suit suit)
        {
            return (Card)((byte)faceValue << 2 | (byte)suit);
        }

        private static readonly Dictionary<FaceValue, string> _displayFaceValues = new Dictionary<FaceValue, string>()
        {
            { Structures.FaceValue.Ace, "A" },
            { Structures.FaceValue.Two, "2" },
            { Structures.FaceValue.Three, "3" },
            { Structures.FaceValue.Four, "4" },
            { Structures.FaceValue.Five, "5" },
            { Structures.FaceValue.Six, "6" },
            { Structures.FaceValue.Seven, "7" },
            { Structures.FaceValue.Eight, "8" },
            { Structures.FaceValue.Nine, "9" },
            { Structures.FaceValue.Ten, "10" },
            { Structures.FaceValue.Jack, "J" },
            { Structures.FaceValue.Queen, "Q" },
            { Structures.FaceValue.King, "K" }
        };

        private static readonly Dictionary<Suit, string> _displaySuits = new Dictionary<Suit, string>()
        {
            { Structures.Suit.Heart, "♥" },
            { Structures.Suit.Spade, "♠" },
            { Structures.Suit.Diamond, "♦" },
            { Structures.Suit.Club, "♣" }
        };

        public static string GetDisplay(this FaceValue self)
        {
            if (_displayFaceValues.TryGetValue(self, out var val)) return val;
            return "?";
        }

        public static string GetDisplay(this Suit self)
        {
            if (_displaySuits.TryGetValue(self, out var val)) return val;
            return "?";
        }

        public static string GetDisplay(this Card self)
        {
            if (self == Card.None) return string.Empty;
            return ((FaceValue)((byte)self >> 2)).GetDisplay() + ((Suit)((byte)self & 0b11)).GetDisplay();
        }
    }
}
