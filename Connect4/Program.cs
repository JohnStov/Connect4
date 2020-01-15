using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Connect4
{
    public enum CellContent
    {
        Empty = 0,
        Red = 1,
        Yellow = 2
    }

    public enum GameState
    {
        GameNotStarted = 0,
        RedWon = 1,
        YellowWon = 2,
        RedToPlay = 3,
        YellowToPlay = 4,
        Draw = 5
    }

    public class Game
    {
        public const int NUMBER_OF_COLUMNS = 7;
        public const int NUMBER_OF_ROWS = 6;

        public CellContent[,] Cells;

        public GameState CurrentState { get; set; }
        public Guid YellowPlayerID { get; set; }
        public Guid RedPlayerID { get; set; }
        public Guid ID { get; set; }
    }

    class Program
    {
        private const string TeamName = "John&James";
        private const string Password = "qwelkjdflgkj";

        static public async Task Main(string[] args)
        {
            Console.WriteLine("Hello Connect4!");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.BaseAddress = new Uri("https://connect4core.azurewebsites.net/");

            var playerId = (await Register(client)).Value;
            Console.WriteLine($"PlayerId = {playerId}");

            if (playerId != null)
            {
                if (await ClearBoard(client, playerId))
                {
                    var finished = false;
                    while (!finished)
                    {
                        var game = await GetGameState(client, playerId);
                        DisplayGame(game, playerId);
                        finished = game.CurrentState == GameState.Draw 
                                   || game.CurrentState == GameState.RedWon 
                                   || game.CurrentState == GameState.YellowWon;

                        if (IsOurTurn(game, playerId))
                            await PlayMove(client, game, playerId, Password);
                    }
                }
            }
        }

        static async Task<bool> PlayMove(HttpClient client, Game game, Guid playerId, string password)
        {
            var move = NextMove(game);

            Console.WriteLine($"Our move is column {move}");

            var config = JsonSerializer.Serialize(new
            {
                PlayerId = playerId,
                Password = password,
                ColumnNumber = move
            });

            var response = await client.PostAsync("api/MakeMove",
                new StringContent(config, Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }

        static int NextMove(Game game)
        {
            var rand = new Random();
            return rand.Next(0, Game.NUMBER_OF_COLUMNS - 1);
        }

        static bool IsOurTurn(Game game, Guid playerId)
        {
            return (game.RedPlayerID == playerId && game.CurrentState == GameState.RedToPlay)
                   || (game.YellowPlayerID == playerId && game.CurrentState == GameState.YellowToPlay);
        }
        
        static async Task<Guid?> Register(HttpClient client)
        {
            var config = JsonSerializer.Serialize(new
            {
                TeamName = TeamName,
                Password = Password
            });

            var response = await client.PostAsync("api/Register",
                new StringContent(config, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Guid.Parse(content.Trim('"'));
            }

            return null;
        }

        static async Task<bool> ClearBoard(HttpClient client, Guid playerId)
        {
            var config = JsonSerializer.Serialize(new
            {
                playerID = playerId
            });

            var response = await client.PostAsync("api/NewGame",
                new StringContent(config, Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }

        static async Task<Game> GetGameState(HttpClient client, Guid playerId)
        {
            var response = await client.GetAsync($"api/GameState/{playerId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Game>(content);
            }

            return null;
        }

        static void DisplayGame(Game game, Guid playerId)
        {
            if (game.RedPlayerID == playerId)
                Console.WriteLine("You are Red");
            else
                Console.WriteLine("You are Yellow");    
            
            switch (game.CurrentState)
            {
                case GameState.GameNotStarted:
                    Console.WriteLine("Game Not Started");
                    break;
                case GameState.RedWon:
                    Console.WriteLine("Red Won");
                    break;
                case GameState.YellowWon:
                    Console.WriteLine("Yellow Won");
                    break;
                case GameState.RedToPlay:
                    Console.WriteLine("Red To Play");
                    break;
                case GameState.YellowToPlay:
                    Console.WriteLine("Yellow To Play");
                    break;
                default:
                    Console.WriteLine("Draw!!!");
                    break;
            }

            if (game.CurrentState != GameState.GameNotStarted)
            {
                for (var y = 1; y <= Game.NUMBER_OF_ROWS; ++y)
                {
                    for (var x = 0; x < Game.NUMBER_OF_COLUMNS; ++x)
                    {
                        switch (game.Cells[x, Game.NUMBER_OF_ROWS - y])
                        {
                            case CellContent.Empty:
                                Console.Write(".");
                                break;
                            case CellContent.Red:
                                Console.Write("R");
                                break;
                            case CellContent.Yellow:
                                Console.Write("Y");
                                break;
                        }
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}
