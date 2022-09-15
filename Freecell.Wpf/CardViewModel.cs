using Freecell.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freecell.Wpf
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public CardViewModel(Card card)
        {
            Card = card;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Card Card { get; }

        public string Suit => Card.Suit()?.GetDisplay();
        public string FaceValue => Card.FaceValue().GetDisplay();

        public bool IsRed => Card.Color() == CardColor.Red;
    }
}
