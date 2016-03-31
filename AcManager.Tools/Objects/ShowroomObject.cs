﻿using System.IO;
using System.Linq;
using System.Windows.Input;
using AcManager.Tools.AcErrors;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Data;
using AcManager.Tools.Lists;
using AcManager.Tools.Managers;
using AcManager.Tools.SemiGui;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Presentation;
using Newtonsoft.Json.Linq;

namespace AcManager.Tools.Objects {
    public class ShowroomObject : AcJsonObjectNew {
        public ShowroomObject(IFileAcManager manager, string id, bool enabled)
                : base(manager, id, enabled) { }

        public override bool HandleChangedFile(string filename) {
            if (base.HandleChangedFile(filename)) {
                return true;
            }

            if (FileUtils.IsAffected(filename, PreviewImage)) {
                RefreshPreviewImage();
                return true;
            }

            if (FileUtils.IsAffected(filename, Kn5Filename)) {
                CheckKn5();
                return true;
            }

            var tail = (Path.GetFileName(filename) ?? "").ToLower();
            if (tail.StartsWith(Id + ".bank") || tail.StartsWith("track.wav")) {
                CheckSound();
            }

            return true;
        }

        protected override AutocompleteValuesList GetTagsList() {
            return SuggestionLists.ShowroomTagsList;
        }

        #region Sound
        private bool _hasSound;

        public bool HasSound {
            get { return _hasSound; }
            private set {
                if (Equals(value, _hasSound)) return;
                _hasSound = value;
                OnPropertyChanged();
            }
        }

        private bool _soundEnabled;

        public bool SoundEnabled {
            get { return _soundEnabled; }
            private set {
                if (Equals(value, _soundEnabled)) return;
                _soundEnabled = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public override string JsonFilename => Path.Combine(Location, "ui", "ui_showroom.json");

        public string PreviewImage => ImageRefreshing ?? Path.Combine(Location, "preview.jpg");
        public void RefreshPreviewImage() {
            OnImageChanged(nameof(PreviewImage));
        }

        public string Kn5Filename => Path.Combine(Location, Id + ".kn5");

        public string SoundbankFilename => Path.Combine(Location, Id + ".bank");

        public string TrackFilename => Path.Combine(Location, "track.wav");

        private void CheckKn5() {
            if (File.Exists(Kn5Filename)) {
                RemoveError(AcErrorType.Showroom_Kn5IsMissing);
            } else {
                AddError(AcErrorType.Showroom_Kn5IsMissing);
            }
        }

        private void CheckSound() {
            SoundEnabled = File.Exists(SoundbankFilename) || File.Exists(TrackFilename);
            HasSound = SoundEnabled || File.Exists(SoundbankFilename + '~') || File.Exists(TrackFilename + '~');
        }

        protected override void LoadOrThrow() {
            base.LoadOrThrow();
            CheckKn5();
            CheckSound();
        }

        protected override void LoadYear(JObject json) {
            base.LoadYear(json);
            if (!Year.HasValue) {
                Year = DataProvider.Instance.ShowroomYears.GetValueOrDefault(Id);
            }
        }

        protected override bool TestIfKunos() {
            return base.TestIfKunos() || (DataProvider.Instance.KunosContent["showrooms"]?.Contains(Id) ?? false);
        }

        public void ToggleSound() {
            var disabledSoundbankFilename = SoundbankFilename + '~';
            var disabledTrackFilename = TrackFilename + '~';
            if (SoundEnabled) {
                if (File.Exists(disabledSoundbankFilename)) {
                    FileUtils.Recycle(disabledSoundbankFilename);
                }

                if (File.Exists(disabledTrackFilename)) {
                    FileUtils.Recycle(disabledTrackFilename);
                }

                if (File.Exists(SoundbankFilename)) {
                    File.Move(SoundbankFilename, disabledSoundbankFilename);
                }

                if (File.Exists(TrackFilename)) {
                    File.Move(TrackFilename, disabledTrackFilename);
                }
            } else {
                if (File.Exists(disabledSoundbankFilename)) {
                    File.Move(disabledSoundbankFilename, SoundbankFilename);
                }

                if (File.Exists(disabledTrackFilename)) {
                    File.Move(disabledTrackFilename, TrackFilename);
                }
            }
        }

        private ICommand _toggleSoundCommand;

        public ICommand ToggleSoundCommand => _toggleSoundCommand ?? (_toggleSoundCommand = new RelayCommand(o => {
            try {
                ToggleSound();
            } catch (ToggleException ex) {
                NonfatalError.Notify(@"Cannot toggle sound",
                    @"Make sure there is no runned app working with showroom's sound files.", ex);
            }
        }, o => HasSound));
    }
}
