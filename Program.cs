using System;
using System.Collections.Generic;
using System.Linq;

namespace Blackjack
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public int Value
        {
            get
            {
                if (Rank >= Rank.Two && Rank <= Rank.Ten) return (int)Rank;
                if (Rank >= Rank.Jack && Rank <= Rank.Queen) return 10;
                if (Rank == Rank.King) return 10;
                return 11; // Ace
            }
        }

        public override string ToString()
        {
            string suitChar = Suit switch
            {
                Suit.Hearts => "\u2665",
                Suit.Diamonds => "\u2666",
                Suit.Clubs => "\u2663",
                Suit.Spades => "\u2660",
                _ => "?"
            };
            string rankStr = Rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                _ => ((int)Rank).ToString()
            };
            return $"[{rankStr}{suitChar}]";
        }
    }

    public class Deck
    {
        private List<Card> _cards;
        private Random _rng = new Random();

        public Deck(int numDecks = 1)
        {
            _cards = new List<Card>();
            for (int d = 0; d < numDecks; d++)
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                    foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                        _cards.Add(new Card(suit, rank));
            Shuffle();
        }

        public void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Deal()
        {
            if (_cards.Count == 0) throw new InvalidOperationException("Deck is empty!");
            var card = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return card;
        }

        public int Remaining => _cards.Count;
    }

    public class Hand
    {
        public List<Card> Cards { get; } = new List<Card>();

        public void Add(Card card) => Cards.Add(card);

        public int Value
        {
            get
            {
                int total = 0;
                int aces = 0;
                foreach (var card in Cards)
                {
                    total += card.Value;
                    if (card.Rank == Rank.Ace) aces++;
                }
                while (total > 21 && aces > 0)
                {
                    total -= 10;
                    aces--;
                }
                return total;
            }
        }

        public bool IsBusted => Value > 21;
        public bool IsBlackjack => Cards.Count == 2 && Value == 21;

        public void Clear() => Cards.Clear();

        public string Display(bool hideFirst = false)
        {
            if (hideFirst && Cards.Count > 0)
                return "[??] " + string.Join(" ", Cards.Skip(1).Select(c => c.ToString()));
            return string.Join(" ", Cards.Select(c => c.ToString()));
        }
    }

    public class Game
    {
        private Deck _deck;
        private Hand _playerHand;
        private Hand _dealerHand;
        private int _playerChips;
        private int _currentBet;
        private int _roundsPlayed;
        private int _wins;
        private int _losses;
        private int _pushes;

        public Game(int startingChips = 100)
        {
            _playerChips = startingChips;
            _deck = new Deck(6); // 6-deck shoe
            _playerHand = new Hand();
            _dealerHand = new Hand();
        }

        public void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            PrintBanner();

            while (_playerChips > 0)
            {
                Console.WriteLine($"\n  Chips: ${_playerChips}  |  Record: {_wins}W-{_losses}L-{_pushes}P  |  Cards left: {_deck.Remaining}");
                Console.WriteLine("  " + new string('-', 50));

                if (_deck.Remaining < 30)
                {
                    Console.WriteLine("\n  Reshuffling the shoe...");
                    _deck = new Deck(6);
                }

                if (!GetBet()) break;
                DealInitial();

                if (_playerHand.IsBlackjack)
                {
                    Console.WriteLine($"\n  Dealer shows: {_dealerHand.Display()} = {_dealerHand.Value}");
                    if (_dealerHand.IsBlackjack)
                    {
                        Console.WriteLine("  Both have Blackjack! Push.");
                        _playerChips += _currentBet;
                        _pushes++;
                    }
                    else
                    {
                        int winAmount = (int)(_currentBet * 1.5);
                        Console.WriteLine($"  BLACKJACK! You win ${winAmount}!");
                        _playerChips += _currentBet + winAmount;
                        _wins++;
                    }
                }
                else
                {
                    if (PlayerTurn())
                    {
                        // Player didn't bust, dealer's turn
                        DealerTurn();
                        ResolveRound();
                    }
                    else
                    {
                        // Player busted
                        Console.WriteLine($"\n  BUST! You lose ${_currentBet}.");
                        _losses++;
                    }
                }

                _playerHand.Clear();
                _dealerHand.Clear();
                _roundsPlayed++;
            }

            if (_playerChips <= 0)
            {
                Console.WriteLine("\n  You're out of chips! Game over.");
            }
            PrintFinalStats();
        }

        private void PrintBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"
  ╔══════════════════════════════════════════════╗
  ║          ♠ ♥ ♣ ♦  B L A C K J A C K  ♦ ♣ ♥ ♠  ║
  ╚══════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private bool GetBet()
        {
            while (true)
            {
                Console.Write($"\n  Place your bet (1-{_playerChips}, or 0 to quit): ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;
                if (input == "0") return false;
                if (int.TryParse(input, out int bet) && bet >= 1 && bet <= _playerChips)
                {
                    _currentBet = bet;
                    _playerChips -= bet;
                    return true;
                }
                Console.WriteLine("  Invalid bet. Try again.");
            }
        }

        private void DealInitial()
        {
            _playerHand.Add(_deck.Deal());
            _dealerHand.Add(_deck.Deal());
            _playerHand.Add(_deck.Deal());
            _dealerHand.Add(_deck.Deal());

            Console.WriteLine($"\n  Dealer: {_dealerHand.Display(hideFirst: true)}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Your hand:  {_playerHand.Display()} = {_playerHand.Value}");
            Console.ResetColor();
        }

        private bool PlayerTurn()
        {
            while (true)
            {
                Console.Write("\n  (H)it, (S)tand, or (D)ouble Down? ");
                string? rawInput = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(rawInput)) continue;
                string input = rawInput.ToUpper();

                switch (input)
                {
                    case "H":
                    case "HIT":
                        _playerHand.Add(_deck.Deal());
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  Your hand:  {_playerHand.Display()} = {_playerHand.Value}");
                        Console.ResetColor();
                        if (_playerHand.IsBusted) return false;
                        break;

                    case "S":
                    case "STAND":
                        return true;

                    case "D":
                    case "DOUBLE":
                    case "DOUBLE DOWN":
                        if (_playerChips >= _currentBet && _playerHand.Cards.Count == 2)
                        {
                            _playerChips -= _currentBet;
                            _currentBet *= 2;
                            Console.WriteLine($"  Doubled! Bet is now ${_currentBet}");
                            _playerHand.Add(_deck.Deal());
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"  Your hand:  {_playerHand.Display()} = {_playerHand.Value}");
                            Console.ResetColor();
                            return !_playerHand.IsBusted;
                        }
                        else if (_playerHand.Cards.Count > 2)
                        {
                            Console.WriteLine("  Can only double down on your first two cards.");
                        }
                        else
                        {
                            Console.WriteLine("  Not enough chips to double down.");
                        }
                        break;

                    default:
                        Console.WriteLine("  Invalid input. Enter H, S, or D.");
                        break;
                }
            }
        }

        private void DealerTurn()
        {
            Console.WriteLine($"\n  Dealer reveals: {_dealerHand.Display()} = {_dealerHand.Value}");
            while (_dealerHand.Value < 17)
            {
                _dealerHand.Add(_deck.Deal());
                Console.WriteLine($"  Dealer hits:   {_dealerHand.Display()} = {_dealerHand.Value}");
            }
        }

        private void ResolveRound()
        {
            int playerVal = _playerHand.Value;
            int dealerVal = _dealerHand.Value;

            Console.WriteLine($"\n  Your total: {playerVal}  |  Dealer total: {dealerVal}");

            if (_dealerHand.IsBusted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Dealer busts! You win ${_currentBet}!");
                Console.ResetColor();
                _playerChips += _currentBet * 2;
                _wins++;
            }
            else if (playerVal > dealerVal)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  You win ${_currentBet}!");
                Console.ResetColor();
                _playerChips += _currentBet * 2;
                _wins++;
            }
            else if (playerVal < dealerVal)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Dealer wins. You lose ${_currentBet}.");
                Console.ResetColor();
                _losses++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Push! Bet returned.");
                Console.ResetColor();
                _playerChips += _currentBet;
                _pushes++;
            }
        }

        private void PrintFinalStats()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  ╔══════════════════════════════════════════╗");
            Console.WriteLine("  ║            FINAL STATISTICS              ║");
            Console.WriteLine("  ╠══════════════════════════════════════════╣");
            Console.WriteLine($"  ║  Rounds Played:  {_roundsPlayed,-22}║");
            Console.WriteLine($"  ║  Wins:           {_wins,-22}║");
            Console.WriteLine($"  ║  Losses:         {_losses,-22}║");
            Console.WriteLine($"  ║  Pushes:         {_pushes,-22}║");
            Console.WriteLine($"  ║  Final Chips:    ${_playerChips,-21}║");
            Console.WriteLine("  ╚══════════════════════════════════════════╝");
            Console.ResetColor();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game(startingChips: 100);
            game.Run();
        }
    }
}
