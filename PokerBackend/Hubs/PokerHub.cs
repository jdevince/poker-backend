using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PokerBackend.Models;
using PokerBackend.Services;
using System.Text.RegularExpressions;
using PokerBackend.Enums;

namespace PokerBackend.Hubs
{
    public class PokerHub : Hub
    {
        private readonly IHubContext<PokerHub> _hubContext;
        private PokerService _pokerService;

        public PokerHub(PokerService pokerService, IHubContext<PokerHub> hubContext)
        {
            _pokerService = pokerService;
            _hubContext = hubContext;
        }

        public string JoinGame(string username, string gameCode)
        {
            gameCode = gameCode.ToUpper();
            Game game = _pokerService.Games.FirstOrDefault(x => x.GameCode == gameCode);

            if (game == null)
            {
                return "That game code does not exist";
            }
            else if (game.Players.Any(x => x.Username.ToUpper() == username.ToUpper()))
            {
                return "That username is already taken for this game";
            }
            else
            {
                Player player = new Player(username, Context.ConnectionId);
                game.Players.Add(player);
                Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
                return "Success";
            }
        }

        public void SitDown(int seatNumber, double buyInAmount)
        {
            Player player = _pokerService.GetCurrentPlayer(Context.ConnectionId);
            player.Seat = seatNumber;
            player.Chips = buyInAmount;
            SendGameStateUpdate();
        }

        public void StartGame()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            new Task(() => { PlayGame(game); }).Start();
        }

        public void GetCurrentGameState()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            _hubContext.Clients.Client(Context.ConnectionId).SendAsync("gameStateChange", game);
        }

        private override Task OnDisconnectedAsync(Exception exception)
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);

            if (game != null)
            {
                Player player = _pokerService.GetCurrentPlayer(Context.ConnectionId);
                game.Players.Remove(player);

                if (game.Players.Count == 0)
                {
                    _pokerService.DeleteGame(game);
                }
                else
                {
                    SendGameStateUpdate();
                }
            }

            _pokerService.CheckForStaleGames(); //Unrelated to this game, but safety check to avoid memory leak

            return base.OnDisconnectedAsync(exception);
        }

        private void PlayGame(Game game)
        {
            while (game.Players.Count > 0)
            {
                PlayHand(game);
            }
        }

        private void PlayHand(Game game)
        {
            var deck = _pokerService.GetFullDeck();
            game.Pot = 0;

            Deal(game, deck);
            DoBidding(game);
            Flop(game, deck);
            DoBidding(game);
            Turn(game, deck);
            DoBidding(game);
            River(game, deck);
            DoBidding(game);
            EvaluateWinner(game);
        }

        private void Deal(Game game, List<Card> deck)
        {
            var players = game.Players.Where(x => x.Seat != null).OrderBy(x => x.Seat);

            for (int i = 0; i < 2; i++)
            {
                foreach (Player player in players)
                {
                    player.Hand[i] = _pokerService.GetCardFromDeck(deck);
                }
            }
        }

        private void DoBidding(Game game)
        {
            _pokerService.ResetForBidding(game);
            
            while(!_pokerService.IsBiddingOver(game))
            {
                Player player = _pokerService.GetNextPlayerToTakeAction(game);
                
                //Wait for player to take action
                for (int i = 0; i < 60; i++)
                {
                    Thread.Sleep(500);
                    if (player.CurrentAction != PlayerActions.None)
                    {
                        break;
                    }
                }

                //Handle no response
                if (player.CurrentAction == PlayerActions.None)
                {
                    bool canCheck = _pokerService.GetHighestBet(game) == 0;
                    if (canCheck)
                    {
                        player.CurrentAction = PlayerActions.Check;
                    }
                    else
                    {
                        Fold(player);
                    }
                }
            }
        }

        public string TakeAction(PlayerActions action, double amount = 0)
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            Player player = _pokerService.GetCurrentPlayer(Context.ConnectionId);

            if (action == PlayerActions.Check)
            {
                bool canCheck = _pokerService.GetHighestBet(game) == 0;
                if (canCheck)
                {
                    player.CurrentAction = PlayerActions.Check;
                    return "Success";
                }
                else
                {
                    return "You cannot check because someone else has bet";
                }
            }
            else if (action == PlayerActions.Bet || action == PlayerActions.Raise)
            {
                double previousHighestBet = _pokerService.GetHighestBet(game);

                if (amount > previousHighestBet)
                {
                    player.CurrentAction = action;
                    player.CurrentBet = amount;
                    player.Chips -= amount;
                    game.Pot += amount;
                    return "Success";
                }
                else
                {
                    return "Your bet is lower than the current highest bet";
                }
            }
            else if (action == PlayerActions.Call)
            {
                double callAmount = _pokerService.GetHighestBet(game);
                if (callAmount > 0)
                {
                    player.CurrentAction = PlayerActions.Call;
                    player.CurrentBet = callAmount;
                    player.Chips -= callAmount;
                    game.Pot += callAmount;
                    return "Success";
                }
                else
                {
                    return "You cannot call because no one else has bet";
                }
            }
            else if (action == PlayerActions.Fold)
            {
                Fold(player);
                return "Success";
            }
            else
            {
                return "Invalid action";
            }
        }

        private void Fold(Player player)
        {
            player.CurrentAction = PlayerActions.Fold;
            player.Hand = null;
        }

        private void Flop(Game game, List<Card> deck)
        {
            for (int i = 0; i < 3; i++)
            {
                game.Board[i] = _pokerService.GetCardFromDeck(deck);
            }
        }

        private void Turn(Game game, List<Card> deck)
        {
            game.Board[3] = _pokerService.GetCardFromDeck(deck);
        }

        private void River(Game game, List<Card> deck)
        {
            game.Board[4] = _pokerService.GetCardFromDeck(deck);
        }

        private void EvaluateWinner(Game game)
        {

        }

        private void SendGameStateUpdate()
        {
            Game game = _pokerService.GetCurrentGame(Context.ConnectionId);
            Clients.Group(game.GameCode).SendAsync("gameStateUpdate", game);
        }
    }
}
