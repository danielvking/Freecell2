using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Structures
{
    public enum FaceValue : byte
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    public enum Suit : byte
    {
        Heart = 0,
        Spade = 1,
        Diamond = 2,
        Club = 3
    }

    public enum CardColor : byte
    {
        Red = 0,
        Black = 1
    }

    public enum Card : byte
    {
        None = 0,
        AceHeart = FaceValue.Ace << 2 | Suit.Heart,
        AceSpade = FaceValue.Ace << 2 | Suit.Spade,
        AceDiamond = FaceValue.Ace << 2 | Suit.Diamond,
        AceClub = FaceValue.Ace << 2 | Suit.Club,
        TwoHeart = FaceValue.Two << 2 | Suit.Heart,
        TwoSpade = FaceValue.Two << 2 | Suit.Spade,
        TwoDiamond = FaceValue.Two << 2 | Suit.Diamond,
        TwoClub = FaceValue.Two << 2 | Suit.Club,
        ThreeHeart = FaceValue.Three << 2 | Suit.Heart,
        ThreeSpade = FaceValue.Three << 2 | Suit.Spade,
        ThreeDiamond = FaceValue.Three << 2 | Suit.Diamond,
        ThreeClub = FaceValue.Three << 2 | Suit.Club,
        FourHeart = FaceValue.Four << 2 | Suit.Heart,
        FourSpade = FaceValue.Four << 2 | Suit.Spade,
        FourDiamond = FaceValue.Four << 2 | Suit.Diamond,
        FourClub = FaceValue.Four << 2 | Suit.Club,
        FiveHeart = FaceValue.Five << 2 | Suit.Heart,
        FiveSpade = FaceValue.Five << 2 | Suit.Spade,
        FiveDiamond = FaceValue.Five << 2 | Suit.Diamond,
        FiveClub = FaceValue.Five << 2 | Suit.Club,
        SixHeart = FaceValue.Six << 2 | Suit.Heart,
        SixSpade = FaceValue.Six << 2 | Suit.Spade,
        SixDiamond = FaceValue.Six << 2 | Suit.Diamond,
        SixClub = FaceValue.Six << 2 | Suit.Club,
        SevenHeart = FaceValue.Seven << 2 | Suit.Heart,
        SevenSpade = FaceValue.Seven << 2 | Suit.Spade,
        SevenDiamond = FaceValue.Seven << 2 | Suit.Diamond,
        SevenClub = FaceValue.Seven << 2 | Suit.Club,
        EightHeart = FaceValue.Eight << 2 | Suit.Heart,
        EightSpade = FaceValue.Eight << 2 | Suit.Spade,
        EightDiamond = FaceValue.Eight << 2 | Suit.Diamond,
        EightClub = FaceValue.Eight << 2 | Suit.Club,
        NineHeart = FaceValue.Nine << 2 | Suit.Heart,
        NineSpade = FaceValue.Nine << 2 | Suit.Spade,
        NineDiamond = FaceValue.Nine << 2 | Suit.Diamond,
        NineClub = FaceValue.Nine << 2 | Suit.Club,
        TenHeart = FaceValue.Ten << 2 | Suit.Heart,
        TenSpade = FaceValue.Ten << 2 | Suit.Spade,
        TenDiamond = FaceValue.Ten << 2 | Suit.Diamond,
        TenClub = FaceValue.Ten << 2 | Suit.Club,
        JackHeart = FaceValue.Jack << 2 | Suit.Heart,
        JackSpade = FaceValue.Jack << 2 | Suit.Spade,
        JackDiamond = FaceValue.Jack << 2 | Suit.Diamond,
        JackClub = FaceValue.Jack << 2 | Suit.Club,
        QueenHeart = FaceValue.Queen << 2 | Suit.Heart,
        QueenSpade = FaceValue.Queen << 2 | Suit.Spade,
        QueenDiamond = FaceValue.Queen << 2 | Suit.Diamond,
        QueenClub = FaceValue.Queen << 2 | Suit.Club,
        KingHeart = FaceValue.King << 2 | Suit.Heart,
        KingSpade = FaceValue.King << 2 | Suit.Spade,
        KingDiamond = FaceValue.King << 2 | Suit.Diamond,
        KingClub = FaceValue.King << 2 | Suit.Club
    }
}
