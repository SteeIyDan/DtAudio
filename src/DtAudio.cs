using System;
using System.Collections.Generic;

namespace SteelyDan
{
    public class DtAudio : MVRScript
    {
        private static readonly char _dirSep = System.IO.Path.DirectorySeparatorChar;
        private static readonly string _dirSounds = $"Custom{_dirSep}Sounds{_dirSep}{nameof(SteelyDan)}{_dirSep}{nameof(DtAudio)}";

        private static readonly string _triggerLip = "LipTrigger";
        private static readonly string _triggerMouth = "MouthTrigger";
        private static readonly string _triggerThroat = "ThroatTrigger";
        private readonly List<string> _triggerIds = new List<string>() { _triggerLip, _triggerMouth, _triggerThroat };
        private List<JSONStorableAction.ActionCallback> _triggerActionStartCallbacks;
        private List<JSONStorableAction.ActionCallback> _triggerActionEndCallbacks;
        private List<string> _startActionReceiverTargets;
        private List<string> _endActionReceiverTargets;
        private List<Pair<TriggerActionDiscrete, TriggerActionDiscrete>> _triggerActions;

        private List<Pair<DateTime, DateTime>> _lastTriggerTime;

        private AudioSourceControl _audioSource;

        private static readonly string _audioClipClassThroat = "Throat";
        private static readonly string _audioClipClassMouth = "Mouth";
        private static readonly string _audioClipClassBreatheMouth = "BreatheMouth";
        private static readonly string _audioClipClassSlurp = "Slurp";
        private static readonly string _audioClipClassLipSmack = "LipSmack";
        private static readonly string _audioClipClassBreatheOutside = "BreatheOutside";
        private static readonly string _audioClipClassBreatheOutsideDeep = "BreatheOutsideDeep";
        private List<AudioClipCollection> _audioClipCollections = new List<AudioClipCollection> 
        { 
            _audioClipClassThroat, 
            _audioClipClassMouth, 
            _audioClipClassBreatheMouth, 
            _audioClipClassSlurp, 
            _audioClipClassLipSmack, 
            _audioClipClassBreatheOutside,
            _audioClipClassBreatheOutsideDeep
        };

        private Stamina _stamina;
        
        public override void Init()
        {
            _triggerActionStartCallbacks = new List<JSONStorableAction.ActionCallback>() { 
                LipTriggerActionStartCallback, 
                MouthTriggerActionStartCallback, 
                ThroatTriggerActionStartCallback 
                };
            _triggerActionEndCallbacks = new List<JSONStorableAction.ActionCallback>() { 
                LipTriggerActionEndCallback,
                MouthTriggerActionEndCallback, 
                ThroatTriggerActionEndCallback 
                };

            _startActionReceiverTargets = new List<string>();
            _endActionReceiverTargets = new List<string>();
            _triggerActions = new List<Pair<TriggerActionDiscrete, TriggerActionDiscrete>>();

            _lastTriggerTime = new List<Pair<DateTime, DateTime>>();

            _stamina = new Stamina();

            var atom = GetContainingAtom();

            if(!atom.GetStorableIDs().Contains("HeadAudioSource"))
            {
                SuperController.LogError($"{nameof(SteelyDan)}.{nameof(DtAudio)}.{nameof(Init)}: Containing atom does not provide the following required audio source: HeadAudioSource");
                return;
            }

            foreach(string id in _triggerIds)
            {
                if(!atom.GetStorableIDs().Contains(id))
                {
                    SuperController.LogError($"{nameof(SteelyDan)}.{nameof(DtAudio)}.{nameof(Init)}: Containing atom does not provide the following required trigger: {id}.");
                    return;
                }
            }

            _audioSource = atom.GetStorableByID("HeadAudioSource") as AudioSourceControl;

            for(int it = 0; it < _triggerIds.Count; ++it)
            {
                var trigger = atom.GetStorableByID(_triggerIds[it]) as CollisionTrigger;
                TriggerActionDiscrete startAction = trigger.trigger.CreateDiscreteActionStartInternal();
                TriggerActionDiscrete endAction = trigger.trigger.CreateDiscreteActionEndInternal();
                _startActionReceiverTargets.Add($"{nameof(DtAudio)} {_triggerIds[it]} Start");
                _endActionReceiverTargets.Add($"{nameof(DtAudio)} {_triggerIds[it]} End");
                RegisterAction(new JSONStorableAction(_startActionReceiverTargets[it], _triggerActionStartCallbacks[it]));
                RegisterAction(new JSONStorableAction(_endActionReceiverTargets[it], _triggerActionEndCallbacks[it]));
                _triggerActions.Add(new Pair<TriggerActionDiscrete, TriggerActionDiscrete>(startAction, endAction));

                _lastTriggerTime.Add(new Pair<DateTime, DateTime>(DateTime.Now, DateTime.Now));
            }
        }

        public void Start()
        {
            Atom atom = GetContainingAtom();
            for(int it = 0; it < _triggerIds.Count; ++it)
            {
                SetupTriggerAction(_triggerActions[it].First, atom, _startActionReceiverTargets[it]);
                SetupTriggerAction(_triggerActions[it].Second, atom, _endActionReceiverTargets[it]);
            }
            SetupAudioClips("Katrina Jade");
        }

        public void OnDisable()
        {
            foreach(var pair in _triggerActions)
            {
                pair.First.Remove();
                pair.Second.Remove();
            }
        }

        private void SetupTriggerAction(TriggerActionDiscrete triggerAction, Atom atom, string receiverTarget)
        {
            triggerAction.receiverAtom = atom;
            triggerAction.receiver = atom.GetStorableByID(atom.GetStorableIDs().Find(x => x.Contains($"{nameof(SteelyDan)}.{nameof(DtAudio)}")));
            triggerAction.receiverTargetName = receiverTarget;
        }

        private void SetupAudioClips(string voiceProfile)
        {
            foreach(AudioClipCollection collection in _audioClipCollections)
            {
                foreach(string directory in SuperController.singleton.GetDirectoriesAtPath($"{_dirSounds}{_dirSep}{voiceProfile}"))
                {
                    if(collection.Name != directory.Split(_dirSep)[directory.Split(_dirSep).ToList().Count - 1]) continue;
                    foreach(string file in SuperController.singleton.GetFilesAtPath($"{_dirSounds}{_dirSep}{voiceProfile}{_dirSep}{collection.Name}"))
                    {
                        collection.AudioClips.Add(LoadAudio(file));
                    }
                }
            }
        }

        private static NamedAudioClip LoadAudio(string path)
        {
            var localPath = SuperController.singleton.NormalizeLoadPath(path);
            var existing = URLAudioClipManager.singleton.GetClip(localPath);
            if (existing != null)
            {
                return existing;
            }

            var clip = URLAudioClipManager.singleton.QueueClip(SuperController.singleton.NormalizeMediaPath(path));
            if (clip == null)
            {
                return null;
            }

            var nac = URLAudioClipManager.singleton.GetClip(clip.uid);
            return nac ?? null;
        }

        private void LipTriggerActionStartCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].First = DateTime.Now;
            _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].Second).TotalMilliseconds * 100 / 1000);
            SuperController.LogMessage($"LipTrigger Start: {_stamina.Current}");
        }

        private void LipTriggerActionEndCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].Second = DateTime.Now;
            if(_lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].First.CompareTo(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].Second) > 0)
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].First).TotalMilliseconds * 70 / 1000);
            }
            else
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].Second).TotalMilliseconds * 70 / 1000);
            }
            SuperController.LogMessage($"LipTrigger End: {_stamina.Current}");

            if(_stamina.Critical)
            {
                AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassBreatheOutsideDeep);
                int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
                var audioClip = collection.AudioClips[index];
                _audioSource.Stop();
                _audioSource.PlayNow(audioClip);
            }
        }

        private void MouthTriggerActionStartCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].First = DateTime.Now;
            if(_lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].First.CompareTo(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].Second) > 0)
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerLip)].First).TotalMilliseconds * 70 / 1000);
            }
            else
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].Second).TotalMilliseconds * 70 / 1000);
            }
            SuperController.LogMessage($"MouthTrigger Start: {_stamina.Current}");

            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassMouth);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            _audioSource.Stop();
            _audioSource.PlayNow(audioClip);
        }

        private void MouthTriggerActionEndCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].Second = DateTime.Now;
            if(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].First.CompareTo(_lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].Second) > 0)
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].First).TotalMilliseconds * 10 / 1000);
            }
            else
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].Second).TotalMilliseconds * 10 / 1000);
            }

            SuperController.LogMessage($"MouthTrigger End: {_stamina.Current}");

            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassSlurp);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            _audioSource.Stop();
            _audioSource.PlayNow(audioClip);
        }

        private void ThroatTriggerActionStartCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].First = DateTime.Now;
            if(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].First.CompareTo(_lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].Second) > 0)
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerMouth)].First).TotalMilliseconds * 10 / 1000);
            }
            else
            {
                _stamina.Add((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].Second).TotalMilliseconds * 10 / 1000);
            }
            _stamina.Subtract(20);
            SuperController.LogMessage($"ThroatTrigger Start: {_stamina.Current}");

            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassThroat);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            _audioSource.Stop();
            _audioSource.PlayNow(audioClip);
        }

        private void ThroatTriggerActionEndCallback()
        {
            _lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].Second = DateTime.Now;
            _stamina.Subtract((int)DateTime.Now.Subtract(_lastTriggerTime[_triggerIds.IndexOf(_triggerThroat)].First).TotalMilliseconds * 150 / 1000);
            SuperController.LogMessage($"ThroatTrigger End: {_stamina.Current}");
        }
    }
}