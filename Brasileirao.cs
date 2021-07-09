using System;
using System.Collections.Generic;

namespace Scraper
{
    class Rodada
    {
        public int Numero { get; set; }
        public List<Jogo> Jogos { get; set; } = new List<Jogo>();
    }

    public class Jogo
    {
        public DateTime? Data { get; set; }
        public string Local { get; set; }
        public Time Mandante { get; set; }
        public int MandanteGols { get; set; }
        public int MandanteGolsPenaltis { get; set; }
        public Time Visitante { get; set; }
        public int VisitanteGols { get; set; }
        public int VisitanteGolsPenaltis { get; set; }
    }

    public class Time
    {
        public string Nome { get; set; }
        public string Sigla { get; set; }
        public Uri Escudo { get; set; }
    }
}
