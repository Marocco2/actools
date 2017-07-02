using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Helpers;
using AcManager.Tools.Objects;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows.Converters;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace AcManager.Controls {
    public static class CommonBatchActions {
        public static readonly BatchAction[] DefaultSet = {
            BatchAction_AddToFavourites.Instance,
            BatchAction_RemoveFromFavourites.Instance,
            BatchAction_SetRating.Instance,

            BatchAction_Delete.Instance,
            BatchAction_Enable.Instance,
            BatchAction_Disable.Instance,
        };

        #region Rating
        public class BatchAction_SetRating : BatchAction<AcObjectNew> {
            public static readonly BatchAction_SetRating Instance = new BatchAction_SetRating();
            public BatchAction_SetRating() : base("Set Rating", "Set rating of several objects at once", "Rating", "Batch.SetRating") { }

            private double _rating = ValuesStorage.GetDouble("_ba.setRating.value", 4d);
            public double Rating {
                get => _rating;
                set {
                    if (Equals(value, _rating)) return;
                    _rating = value;
                    ValuesStorage.Set("_ba.setRating.value", value);
                    OnPropertyChanged();
                }
            }

            private bool _removeRating = ValuesStorage.GetBool("_ba.setRating.remove", true);
            public bool RemoveRating {
                get => _removeRating;
                set {
                    if (Equals(value, _removeRating)) return;
                    _removeRating = value;
                    ValuesStorage.Set("_ba.setRating.remove", value);
                    OnPropertyChanged();
                }
            }

            protected override void ApplyOverride(AcObjectNew obj) {
                if (RemoveRating) {
                    obj.Rating = null;
                } else {
                    obj.Rating = Rating;
                }
            }
        }

        public class BatchAction_AddToFavourites : BatchAction<AcObjectNew> {
            public static readonly BatchAction_AddToFavourites Instance = new BatchAction_AddToFavourites();
            public BatchAction_AddToFavourites() : base("Add To Favourites", "Add several objects at once", "Rating", null) { }

            protected override void ApplyOverride(AcObjectNew obj) {
                obj.IsFavourite = true;
            }
        }

        public class BatchAction_RemoveFromFavourites : BatchAction<AcObjectNew> {
            public static readonly BatchAction_RemoveFromFavourites Instance = new BatchAction_RemoveFromFavourites();
            public BatchAction_RemoveFromFavourites() : base("Remove From Favourites", "Remove several objects at once", "Rating", null) { }

            protected override void ApplyOverride(AcObjectNew obj) {
                obj.IsFavourite = false;
            }
        }
        #endregion

        #region Disabling and removal
        public class BatchAction_Delete : BatchAction<AcCommonObject> {
            public static readonly BatchAction_Delete Instance = new BatchAction_Delete();
            public BatchAction_Delete() : base("Remove", "Remove to the Recycle Bin", "Files", null) { }

            public override Task ApplyAsync(IList list, IProgress<AsyncProgressEntry> progress, CancellationToken cancellation) {
                var objs = OfType(list).ToList();
                if (objs.Count == 0) return Task.Delay(0);

                var manager = objs.First().FileAcManager;
                return manager.DeleteAsync(objs.Select(x => x.Id));
            }
        }

        public class BatchAction_Enable : BatchAction<AcCommonObject> {
            public static readonly BatchAction_Enable Instance = new BatchAction_Enable();
            public BatchAction_Enable() : base("Enable", "Enable disabled objects", "Files", null) { }

            public override Task ApplyAsync(IList list, IProgress<AsyncProgressEntry> progress, CancellationToken cancellation) {
                var objs = OfType(list).ToList();
                if (objs.Count == 0) return Task.Delay(0);

                var manager = objs.First().FileAcManager;
                return manager.ToggleAsync(objs.Select(x => x.Id), true);
            }
        }

        public class BatchAction_Disable : BatchAction<AcCommonObject> {
            public static readonly BatchAction_Disable Instance = new BatchAction_Disable();
            public BatchAction_Disable() : base("Disable", "Disable enabled objects", "Files", null) { }

            public override Task ApplyAsync(IList list, IProgress<AsyncProgressEntry> progress, CancellationToken cancellation) {
                var objs = OfType(list).ToList();
                if (objs.Count == 0) return Task.Delay(0);

                var manager = objs.First().FileAcManager;
                return manager.ToggleAsync(objs.Select(x => x.Id), false);
            }
        }
        #endregion

        #region Miscellaneous
        public abstract class BatchAction_Pack<T> : BatchAction<T> where T : AcCommonObject {
            protected BatchAction_Pack([CanBeNull] string paramsTemplateKey) : base("Pack", "Pack only important files", null, paramsTemplateKey) {
                InternalWaitingDialog = true;
            }

            protected abstract AcCommonObject.AcCommonObjectPackerParams GetParams();

            public override async Task ApplyAsync(IList list, IProgress<AsyncProgressEntry> progress, CancellationToken cancellation) {
                try {
                    var objs = OfType(list).ToList();
                    if (objs.Count == 0) return;

                    var last = $"-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
                    var name = objs.Count == 1 ? $"{objs[0]}-{(objs[0] as IAcObjectVersionInformation)?.Version ?? "0"}{last}" :
                            $"{objs.Select(x => x.Id).OrderBy(x => x).JoinToString('-')}{last}";
                    if (name.Length > 160) {
                        name = name.Substring(0, 160 - last.Length) + last;
                    }

                    var dialog = new SaveFileDialog {
                        Title = objs.Count == 1 ? $"Pack {objs[0].DisplayName}" : $"Pack {objs.Count} {PluralizingConverter.Pluralize(objs.Count, "Object")}",
                        InitialDirectory = ValuesStorage.GetString("_packDir"),
                        Filter = FileDialogFilters.ZipFilter,
                        DefaultExt = ".zip",
                        FileName = name
                    };

                    if (dialog.ShowDialog() != true) return;

                    using (var waiting = WaitingDialog.Create("Packing…")) {
                        await Task.Run(() => {
                            ValuesStorage.Set("_packDir", Path.GetDirectoryName(dialog.FileName));
                            using (var output = File.Create(dialog.FileName)) {
                                AcCommonObject.Pack(objs, output,
                                        new Progress<string>(x => waiting.Report(AsyncProgressEntry.FromStringIndetermitate($"Packing: {x}…"))),
                                        GetParams());
                            }

                            WindowsHelper.ViewFile(dialog.FileName);
                        });
                    }
                } catch (Exception e) {
                    NonfatalError.Notify("Can’t pack", e);
                }
            }
        }
        #endregion
    }
}