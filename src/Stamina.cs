namespace SteelyDan
{
    class Stamina
    {
        private int _max, _min;
        private int _current;
        private int _critical;

        public bool Critical { get; private set; }
        public int Current { get { return _current; } }

        public Stamina(int min = 0, int max = 1000, int current = 1000, int critical = 600)
        {
            _max = max;
            _min = min;
            _current = current;
            _critical = critical;

            UpdateCritical();
        }

        public void Add(int amount)
        {
            int old = _current;
            _current += amount;
            if(_current > _max || _current < old)
            {
                _current = _max;
            }
            UpdateCritical();
        }

        public void Subtract(int amount)
        {
            int old = _current;
            _current -= amount;
            if(_current < _min || _current > old)
            {
                _current = _min;
            }
            UpdateCritical();
        }

        private void UpdateCritical()
        {
            Critical = _current <= _critical;
        }
    }
}