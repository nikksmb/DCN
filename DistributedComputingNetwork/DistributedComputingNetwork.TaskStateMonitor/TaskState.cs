using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedComputingNetwork.TaskStateMonitor
{
    public class TaskState
    {
        public IProgress<int> PercentState { get; set; }
        public IProgress<string> Message { get; set; }

        private volatile int _currentState;
        public int CurrentState {
            get { return _currentState;}
            set
            {
                _currentState = value;
                UpdateState();
            }
        }

        private volatile int _maxCount;
        public int MaxCount {
            get { return _maxCount;}
            set
            {
                _maxCount = value;
                _currentState = 0;
                UpdateState();
            }
        }

        private bool _isActive;
        public bool IsActive {
            get { return _isActive; }
            set
            {
                _isActive = value;
                UpdateState();
            }
        }

        public string StateMessage {
            get
            {
                if (IsActive)
                    return _stateMessage;
                return _inactive;
            }
            set { _stateMessage = value; }
        }
        private string _stateMessage;

        private string _inactive = "Inactive";

        public void Reset()
        {
            CurrentState = 0;
            MaxCount = 0;
            IsActive = false;
        }

        private void UpdateState()
        {
            Message?.Report(StateMessage);
            int state = 0;
            if (IsActive)
                state = (int) ((double) CurrentState/MaxCount*100);
            if (state > 100)
                state = 100;
            if (state >= 0)
                PercentState?.Report(state);
        }
    }
}
