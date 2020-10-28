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

        private List<NamedAudioClip> _audioClips;
        private readonly List<string> _audioClipTypeNames = new List<string>() { "Throat" };
        
        public override void Init()
        {
            _triggerActionStartCallbacks = new List<JSONStorableAction.ActionCallback>() { LipTriggerActionStartCallback, MouthTriggerActionStartCallback, ThroatTriggerActionStartCallback };
            _triggerActionEndCallbacks = new List<JSONStorableAction.ActionCallback>() { LipTriggerActionEndCallback, MouthTriggerActionEndCallback, ThroatTriggerActionEndCallback };

            _startActionReceiverTargets = new List<string>();
            _endActionReceiverTargets = new List<string>();
            _triggerActions = new List<Pair<TriggerActionDiscrete, TriggerActionDiscrete>>();

            _audioClips = new List<NamedAudioClip>();

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
            foreach(string clipTypeName in _audioClipTypeNames)
            {
                foreach(string file in SuperController.singleton.GetFilesAtPath($"Custom\\Sounds\\SteelyDan\\DtAudio\\{voiceProfile}\\{clipTypeName}"))
                {
                    _audioClips.Add(LoadAudio(file));
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

        }

        private void MouthTriggerActionEndCallback()
        {

        }

        private void ThroatTriggerActionStartCallback()
        {
            int index = UnityEngine.Random.Range(0, _audioClips.Count);
            var audioClip = _audioClips[index];
            SuperController.LogMessage($"{index}");
            _audioSource.PlayNow(audioClip);
        }

        private void ThroatTriggerActionEndCallback()
        {

        }
    }
}