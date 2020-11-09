using System;
using System.Collections.Generic;

namespace SteelyDan
{
    public class DtAudio : MVRScript
    {
        private readonly List<string> _triggerIds = new List<string>() { "LipTrigger", "MouthTrigger", "ThroatTrigger" };
        private List<JSONStorableAction.ActionCallback> _triggerActionStartCallbacks;
        private List<JSONStorableAction.ActionCallback> _triggerActionEndCallbacks;
        private List<string> _startActionReceiverTargets;
        private List<string> _endActionReceiverTargets;
        private List<Pair<TriggerActionDiscrete, TriggerActionDiscrete>> _triggerActions;

        private AudioSourceControl _audioSource;

        private static readonly string _audioClipClassThroat = "Throat";
        private static readonly string _audioClipClassMouth = "Mouth";
        private static readonly string _audioClipClassBreatheMouth = "BreatheMouth";
        private static readonly string _audioClipClassSlurp = "Slurp";
        private static readonly string _audioClipClassLipSmack = "LipSmack";
        private static readonly string _audioClipClassBreatheOutside = "BreatheOutside";
        private readonly List<AudioClipCollection> _audioClipCollections = new List<AudioClipCollection> 
        { 
            _audioClipClassThroat, _audioClipClassMouth, _audioClipClassBreatheMouth, _audioClipClassSlurp, _audioClipClassLipSmack, _audioClipClassBreatheOutside 
        };
        
        public override void Init()
        {
            _triggerActionStartCallbacks = new List<JSONStorableAction.ActionCallback>() { LipTriggerActionStartCallback, MouthTriggerActionStartCallback, ThroatTriggerActionStartCallback };
            _triggerActionEndCallbacks = new List<JSONStorableAction.ActionCallback>() { LipTriggerActionEndCallback, MouthTriggerActionEndCallback, ThroatTriggerActionEndCallback };

            _startActionReceiverTargets = new List<string>();
            _endActionReceiverTargets = new List<string>();
            _triggerActions = new List<Pair<TriggerActionDiscrete, TriggerActionDiscrete>>();

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
                foreach(string file in SuperController.singleton.GetFilesAtPath($"Custom\\Sounds\\SteelyDan\\DtAudio\\{voiceProfile}\\{collection.Name}"))
                {
                    collection.AudioClips.Add(LoadAudio(file));
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

        }

        private void LipTriggerActionEndCallback()
        {

        }

        private void MouthTriggerActionStartCallback()
        {
            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassMouth);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            SuperController.LogMessage($"{_audioClipClassMouth} {audioClip.displayName}");
            _audioSource.PlayNow(audioClip);
        }

        private void MouthTriggerActionEndCallback()
        {
            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassSlurp);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            SuperController.LogMessage($"{_audioClipClassSlurp} {audioClip.displayName}");
            _audioSource.PlayNow(audioClip);
        }

        private void ThroatTriggerActionStartCallback()
        {
            AudioClipCollection collection = _audioClipCollections.Find(x => x.Name == _audioClipClassThroat);
            int index = UnityEngine.Random.Range(0, collection.AudioClips.Count);
            var audioClip = collection.AudioClips[index];
            SuperController.LogMessage($"{_audioClipClassThroat} {audioClip.displayName}");
            _audioSource.PlayNow(audioClip);
        }

        private void ThroatTriggerActionEndCallback()
        {

        }
    }
}