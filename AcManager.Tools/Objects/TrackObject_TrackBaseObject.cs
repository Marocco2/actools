﻿using System.Linq;
using System.Text;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Data;
using AcManager.Tools.Helpers;
using AcManager.Tools.Lists;
using AcManager.Tools.Managers;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using Newtonsoft.Json.Linq;

namespace AcManager.Tools.Objects {
    public abstract partial class TrackBaseObject : AcJsonObjectNew {
        protected TrackBaseObject(IFileAcManager manager, string id, bool enabled)
                : base(manager, id, enabled) { }

        public override void PastLoad() {
            base.PastLoad();

            if (Enabled) {
                SuggestionLists.CitiesList.AddUnique(City);
            }
        }

        public abstract TrackObject MainTrackObject { get; }

        public abstract string LayoutId { get; }

        public abstract string IdWithLayout { get; }

        public abstract string LayoutName { get; set; }

        #region Properties & Specifications
        private string _city;
        public string City {
            get { return _city; }
            set {
                if (value == _city) return;
                _city = value;
                OnPropertyChanged(nameof(City));

                Changed = true;
            }
        }

        private GeoTagsEntry _geoTags;
        public GeoTagsEntry GeoTags {
            get { return _geoTags; }
            set {
                if (value == _geoTags) return;
                _geoTags = value;
                OnPropertyChanged(nameof(GeoTags));

                Changed = true;
            }
        }

        private string _specsLength;
        public string SpecsLength {
            get { return _specsLength; }
            set {
                if (value == _specsLength) return;
                _specsLength = value;
                OnPropertyChanged(nameof(SpecsLength));

                Changed = true;
            }
        }

        private string _specsWidth;
        public string SpecsWidth {
            get { return _specsWidth; }
            set {
                if (value == _specsWidth) return;
                _specsWidth = value;
                OnPropertyChanged(nameof(SpecsWidth));

                Changed = true;
            }
        }

        private string _specsPitboxes;
        public string SpecsPitboxes {
            get { return _specsPitboxes; }
            set {
                if (value == _specsPitboxes) return;
                _specsPitboxes = value;
                OnPropertyChanged(nameof(SpecsPitboxes));

                Changed = true;
            }
        }

        public string SpecsInfoDisplay {
            get {
                var result = new StringBuilder();
                int pitboxes;
                if (!FlexibleParser.TryParseInt(SpecsPitboxes, out pitboxes)) {
                    pitboxes = 99;
                }

                foreach (var val in new[] {
                    SpecsLength, 
                    SpecsWidth, 
                    string.IsNullOrWhiteSpace(SpecsPitboxes) ? "" : SpecsPitboxes + LocalizationHelper.MultiplyForm(pitboxes, @" pit", @" pits")
                }.Where(val => !string.IsNullOrWhiteSpace(val))) {
                    if (result.Length > 0) {
                        result.Append(@", ");
                    }

                    result.Append(val);
                }

                return result.Length > 0 ? result.ToString() : null;
            }
        }
        #endregion

        protected override AutocompleteValuesList GetTagsList() {
            return SuggestionLists.TrackTagsList;
        }

        protected override void LoadData(JObject json) {
            base.LoadData(json);
            
            City = json.GetStringValueOnly("city");
            GeoTags = json.GetGeoTagsValueOnly("geotags");

            if (Country == null) {
                foreach (var country in Tags.Select(AcStringValues.CountryFromTag).Where(x => x != null)) {
                    Country = country;
                    break;
                }
            }

            SpecsLength = json.GetStringValueOnly("length");
            SpecsWidth = json.GetStringValueOnly("width");
            SpecsPitboxes = json.GetStringValueOnly("pitboxes");
        }

        protected override void LoadYear(JObject json) {
            base.LoadYear(json);
            if (!Year.HasValue) {
                Year = DataProvider.Instance.TrackYears.GetValueOrDefault(Id);
            }
        }

        protected override bool TestIfKunos() {
            return base.TestIfKunos() || (DataProvider.Instance.KunosContent["tracks"]?.Contains(Id) ?? false);
        }

        public override void SaveData(JObject json) {
            base.SaveData(json);
            
            json["city"] = City;

            if (GeoTags != null) {
                json["geotags"] = GeoTags.ToJObject();
            } else {
                json.Remove("geotags");
            }

            json["length"] = SpecsLength;
            json["width"] = SpecsWidth;
            json["pitboxes"] = SpecsPitboxes;
        }

        public abstract string PreviewImage { get; }

        public abstract string OutlineImage { get; }
    }
}
