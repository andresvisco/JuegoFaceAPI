using Entities;
using Entities.Enums;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class GameService
    {
        private static GameService _instance;
        private static object syncLock = new object();
        private FaceServiceClient _faceClient = null;
        private int _score = 0;
        private int _maxTime = 15;

        public int Score { get { return _score; } }
        private GameStateEnum _state = GameStateEnum.NotInitialized;
        public GameStateEnum State { get { return _state; } }

        private List<Player> _players { get; set; } = new List<Player>();
        private List<PlayerRect> _localPlayers { get; set; } = new List<PlayerRect>();
        public List<Player> Players { get { return _players; } }
        private Timer _timer;
        private int _timeElapsed = 0;
        // public int TimeElapsed { get { return _timeElapsed; } }
        public int TimeElapsed { get { return (_timeElapsed - _maxTime); } }


        public int MaxTime { set { _maxTime = value; } }

        private GameService(FaceServiceClient faceClient)
        {
            _faceClient = faceClient;
            _state = GameStateEnum.Initialized;
        }

        public static GameService Init(FaceServiceClient faceClient)
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new GameService(faceClient);
                    }
                }
            }

            return _instance;
        }

        public void StartGame()
        {
            _state = GameStateEnum.Started;
            StartTimer();

        }
        public void RestartGame()
        {
            _state = GameStateEnum.Initialized;
            _score = 0;
            _players = new List<Player>();
            _timeElapsed = 0;
            StopTimer();


        }
        public void EndGame()
        {
            _state = GameStateEnum.Finished;
            StopTimer();

        }
        private void OnTimer(object state)
        {
            _timeElapsed++;

            if (_timeElapsed > _maxTime)
            {
                EndGame();

            }
        }

        public void StartTimer()
        {

            _timeElapsed = 0;

            _timer = new Timer(OnTimer, null, TimeSpan.Zero, new TimeSpan(0, 0, 1));



        }
        public void StopTimer()
        {
            if (_timer != null)
            {

                _timer.Dispose();
            }
        }

        /// <summary>
        /// Procesa el frame pasado por parametro para detectar los jugadores y mantener
        /// el listado de jugadores en el juego.
        /// y las detecciones locales.
        /// </summary>
        /// <param name="frame"> frame capturado para analizar los rostros</param>
        /// <param name="localFaces"> caras detectadas localmente </param>
        /// <returns></returns>
        public async Task<Face[]> ProcessFrame(MemoryStream frame, Rect[] localFaces)
        {
            Face[] faces = await AnalyzeFrame(frame, localFaces);
            //ProcessResults(faces);
            return faces;
        }

        public async Task<Face[]> ProcessFrameMock(MemoryStream frame, Rect[] localFaces)
        {
            Face[] faces = null;
            if (localFaces != null && localFaces.Count() > 0)
            {
                localFaces = localFaces.OrderBy(r => r.Left + 0.5 * r.Width).ToArray();
                faces = new Face[localFaces.Length];
                for (int i = 0; i < localFaces.Length; i++)
                {
                    Face face = new Face
                    {
                        FaceId = new Guid(String.Format("ddddddddddddddddddddddddddddddd{0}", i)),
                        FaceRectangle = new FaceRectangle { Left = localFaces[i].Left, Top = localFaces[i].Top, Width = localFaces[i].Width, Height = localFaces[i].Height },
                        FaceAttributes = new FaceAttributes()
                        {
                            Emotion = new EmotionScores()
                            {
                                Anger = 0,
                                Contempt = 0,
                                Disgust = 0,
                                Fear = 0,
                                Happiness = 1,
                                Neutral = 0,
                                Sadness = 0,
                                Surprise = 0
                            }
                        }
                    };

                    faces[i] = face;

                }

                if (_players.Count > 0)
                {
                    faces = faces.Select(f => AnalyzePlayerFaceMock(f)).ToArray();
                }

            }
            else
            {
                faces = new Face[0];
            }

            return faces;
        }
        public void ProcessLocalFaces(OpenCvSharp.Rect[] localFaces)
        {
            if (_players.Count > 0)
            {
                int offset = 30;

                foreach (Player player in _players)
                {
                    OpenCvSharp.Rect rect = localFaces.Where(r => r.Left.IsBetween(player.Position.Left - offset, player.Position.Left + offset) &&
                    r.Top.IsBetween(player.Position.Top - offset, player.Position.Top + offset) &&
                    r.Width.IsBetween(player.Position.Width - offset, player.Position.Width + offset) &&
                    r.Height.IsBetween(player.Position.Height - offset, player.Position.Height + offset)).FirstOrDefault();

                    if (rect != null)
                    {
                        player.Position = new FaceRectangle { Left = rect.Left, Top = rect.Top, Width = rect.Width, Height = rect.Height };
                    }

                }
            }
        }
        /// <summary>
        /// Procesa el resultado de los rostros detectados y mantiene la lista de jugadores en juego
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        public void ProcessResults(Face[] faces)
        {
            //Obtengo una lista de jugadores detectados
            List<Player> players = faces.Select(f => new Player(f.FaceId, f.FaceRectangle, f.FaceAttributes)).ToList();
            List<Player> newPlayers = newPlayers = players.Where(p => !_players.Any(_p => _p.Id == p.Id)).ToList();

            //Si el juego esta en estado inicializdo, puedo agregar nuevos jugadores
            if (_state == GameStateEnum.Initialized && newPlayers.Count > 0)
            {
                _players.AddRange(newPlayers);
            }

            //Si el juego esta en estado inicializado o ya comenzado
            if (_state == GameStateEnum.Initialized || _state == GameStateEnum.Started || _state == GameStateEnum.Finished)
            {
                List<Player> updatedPlayers = players.Where(p => _players.Any(_p => _p.Id == p.Id) && !newPlayers.Any(np => np.Id == p.Id)).ToList();

                if (updatedPlayers.Count > 0)
                {
                    foreach (Player player in updatedPlayers)
                    {
                        Player playerToUpdate = _players.FirstOrDefault(_p => _p.Id == player.Id);

                        if (playerToUpdate != null)
                        {
                            //Actualizo jugador, si el juego esta inicializado, acumulo score
                            playerToUpdate.Update(player);

                            if (_state == GameStateEnum.Started)
                            {
                                playerToUpdate.AccumulateScore();
                            }

                        }
                    }
                }

                //Reseteo la posicion de los jugadores que no aparecen en la lista

                //Si no se detectaron rostros actualizo las posisiones de los usuarios a 0;
                List<Player> resetPlayers = _players.Where(_p => !players.Any(p => p.Id == _p.Id)).ToList();
                foreach (Player player in resetPlayers)
                {
                    Player playerToUpdate = _players.FirstOrDefault(_p => _p.Id == player.Id);
                    if (playerToUpdate != null)
                    {
                        playerToUpdate.ResetPosition();
                    }
                }

                //  AccumulateScore();

            }
        }



        private void AccumulateScore()
        {
            if (_players.Count > 0)
            {
                _score = _players.Sum(p => p.Score) / _players.Count;
            }
            else
            {
                _score = _players.Sum(p => p.Score);
            }
        }

        /// <summary>
        /// Analiza el frame y devuelve un array de caras detectadas
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="localFaces"></param>
        /// <returns></returns>
        private async Task<Face[]> AnalyzeFrame(MemoryStream frame, Rect[] localFaces)
        {
            var attrs = new List<FaceAttributeType> { FaceAttributeType.Age,
                FaceAttributeType.Gender, FaceAttributeType.HeadPose, FaceAttributeType.Emotion };

            Face[] faces = null;

            if (localFaces != null && localFaces.Count() > 0)
            {
                
                faces = await _faceClient.DetectAsync(frame, returnFaceAttributes: attrs);
                if (_players.Count() > 0)
                {
                    List<Task<Face>> updatedFacesTask = faces.Select(f => AnalyzePlayerFace(f)).ToList();
                    Face[] updatedFaces = await Task.WhenAll(updatedFacesTask);
                    faces = updatedFaces;
                }
            }
            else
            {
                faces = new Face[0];
            }

            return faces;
        }
        /// <summary>
        /// Analiza una cara detectada en particular para validar si ya se encuentra actualmente en la lista de jugadores
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private async Task<Face> AnalyzePlayerFace(Face face)
        {
            try
            {
                Guid[] playersId = _players.Select(p => p.Id).ToArray();
                Coincidence coincidence = await FindSimilarFaceAsync(face.FaceId, playersId, FindSimilarMatchMode.matchFace);
                face.FaceId = (coincidence.MatchId != Guid.Empty) ? coincidence.MatchId : coincidence.NewId;

                return face;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private Face AnalyzePlayerFaceMock(Face face)
        {
            int offset = 30;
            Player player = _players.Where(r => r.Position.Left.IsBetween(face.FaceRectangle.Left - offset, face.FaceRectangle.Left + offset) &&
               r.Position.Top.IsBetween(face.FaceRectangle.Top - offset, face.FaceRectangle.Top + offset) &&
               r.Position.Width.IsBetween(face.FaceRectangle.Width - offset, face.FaceRectangle.Width + offset) &&
               r.Position.Height.IsBetween(face.FaceRectangle.Height - offset, face.FaceRectangle.Height + offset)).FirstOrDefault();

            if (player != null)
            {
                face.FaceId = player.Id;
            }

            return face;
        }


        /// <summary>
        /// Busca si existen rostros actualmente registrados.
        /// </summary>
        /// <param name="faceId"></param>
        /// <param name="facesIds"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private async Task<Coincidence> FindSimilarFaceAsync(Guid faceId, Guid[] facesIds, FindSimilarMatchMode mode)
        {
            try
            {
                SimilarFace[] similarFaces = await _faceClient.FindSimilarAsync(faceId, facesIds, mode);

                SimilarFace similarFace = similarFaces.ToList().Where(x => x.Confidence > 0.30).FirstOrDefault();
                return new Entities.Coincidence
                {
                    MatchId = (similarFace != null) ? similarFace.FaceId : Guid.Empty,
                    Confidence = (similarFace != null) ? similarFace.Confidence : 0,
                    NewId = faceId

                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public static class ExtensionMethods
    {
        public static bool IsBetween(this int value, int start, int end)
        {
            return (start <= value && value <= end);
        }
    }
}
