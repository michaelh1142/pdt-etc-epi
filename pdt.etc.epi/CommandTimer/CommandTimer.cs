using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace pdt.etc.epi.commandTimer
{
    public class CommandTimer
    {

        private CTimer cmdTimer;
        private ushort countDownTime;


        public bool running;

        
        //events
        public event EventHandler TimerCompleted;
        public event EventHandler TimerOneMinute;
        public event EventHandler TimerUpdate;



        //eventHandler
        public void RaiseEvent_TimerCompleted()
        {
            if (TimerCompleted != null)
                TimerCompleted(this, EventArgs.Empty);
        }

        public void RaiseEvent_TimerOneMinute()
        {
            if (TimerOneMinute != null)
                TimerOneMinute(this, EventArgs.Empty);
        }

        public void RaiseEvent_TimerUpdate()
        {
            if (TimerUpdate != null)
                TimerUpdate(this, EventArgs.Empty);
        }

        public ushort ResumeTimer()
        {
            if (!(cmdTimer.Disposed))
            {
                cmdTimer.Reset(1000, 1000);           
            }
            return (0);
        }


        public ushort StartTimer(ushort timeInSeconds)
        {

            countDownTime = timeInSeconds;  // set count down time.
            if (!(cmdTimer == null))      // Check if timer is null - not defined first time called 
            {
                if (cmdTimer.Disposed)    // if timer has been disposed because it was stopped need to new
                {
                    cmdTimer = new CTimer(this.queueTimerCallBack, null, 1000, 1000);
                    
                }
                else
                {
                    cmdTimer.Reset(1000, 1000); // timer in progress reset    
                }
            }
            else
            {
                cmdTimer = new CTimer(this.queueTimerCallBack, null, 1000, 1000);               
            }

            running = true;
            return (1);

        }


        public ushort StopTimer()
        {           
            cmdTimer.Stop();
            cmdTimer.Dispose();
            countDownTime = 0;

            running = false;

            return (0);
        
        }

        public void SkipTimer()
        {
            countDownTime = 0;
            cmdTimer.Reset();

        }


        public ushort PauseTimer()
        {
            cmdTimer.Stop();
            return (1);
            
        }

        public string CurrentTime()
        {
            return (String.Format("{0}:{1}", (countDownTime / 60).ToString("D2"), (countDownTime % 60).ToString("D2")));
        }


        public void queueTimerCallBack(object state)
        {
            if(countDownTime > 0)
                countDownTime--;
            RaiseEvent_TimerUpdate();
            //CrestronConsole.PrintLine("Time Remaining: {0}:{1}", (countDownTime / 60).ToString("D2"), (countDownTime % 60).ToString("D2"));
            if (countDownTime == 60)
            {
                RaiseEvent_TimerOneMinute();
            }
            else if (countDownTime == 0)
            {
                this.StopTimer();
                RaiseEvent_TimerCompleted();
            }
        }
    }

}