using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EmotionUI
{
    public class TimerText
    {
        private static int rangeSeconds = 1000;
        private Timer timer;
        public int contador;
        private Action action;

        public TimerText(int seconds, Action action)
        {
            timer = new Timer(changeState);
            contador = ++seconds;
            this.action = action;
        }

        public void Start()
        {
            timer.Change(rangeSeconds, rangeSeconds);
        }

        private void changeState(Object stateInfo)
        {
            contador--;
            if (contador > -1)
            {
                action();
            }
            if (contador == -1)
            {
                timer.Dispose();
            }
        }
    }
}
