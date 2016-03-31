using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcManager.Tools.AcObjectsNew;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Tools.AcManagersNew {
    public enum AsyncScanManagerStatus {
        Error, Loading, Ready
    }

    public abstract class AsyncScanAcManager<T> : BaseAcManager<T> where T : AcObjectNew {

        private AsyncScanManagerStatus _status;

        public AsyncScanManagerStatus Status {
            get { return _status; }
            set {
                if (Equals(value, _status)) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage;

        public string ErrorMessage {
            get { return _errorMessage; }
            set {
                if (Equals(value, _errorMessage)) return;
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        private Task _scanAsyncTask;
        private CancellationTokenSource _cancellationTokenSource;

        public async Task ScanAsync() {
            var scanned = IsScanned;
            lock (InnerWrappersList) {
                if (IsScanned && !scanned) {
                    return;
                }
            }

            using (_cancellationTokenSource = new CancellationTokenSource()) {
                await (_scanAsyncTask ?? (_scanAsyncTask = ActualScanAsync(_cancellationTokenSource.Token)));
                _scanAsyncTask = null;
            }
            _cancellationTokenSource = null;
        }

        public override void ActualScan() {
            _cancellationTokenSource?.Cancel();
            _scanAsyncTask = null;

            try {
                base.ActualScan();
                Status = AsyncScanManagerStatus.Ready;
            } catch (Exception e) {
                Status = AsyncScanManagerStatus.Error;
                ErrorMessage = e.Message;
            }
        }

        [ItemNotNull]
        protected virtual Task<IEnumerable<AcPlaceholderNew>> ScanInnerAsync() {
            return Task.Run(() => ScanInner());
        }

        public async Task ActualScanAsync(CancellationToken cancellation) {
            Status = AsyncScanManagerStatus.Loading;
            InnerWrappersList.Clear();

            IEnumerable<AcPlaceholderNew> entries;
            try {
                entries = await ScanInnerAsync();
            } catch (Exception e) {
                if (cancellation.IsCancellationRequested) return;

                InnerWrappersList.Clear();
                Status = AsyncScanManagerStatus.Error;
                ErrorMessage = e.Message;
                return;
            }

            if (cancellation.IsCancellationRequested) return;
            if (IsScanning) throw new Exception("Scanning already in process");

            IsLoaded = false;
            IsScanning = true;

            try {
                foreach (var obj in InnerWrappersList.Select(x => x.Value).OfType<T>()) {
                    obj.Outdate();
                }
                
                InnerWrappersList.AddRange(entries.Select(x => new AcItemWrapper(this, x)));
                Status = AsyncScanManagerStatus.Ready;
            } catch (Exception e) {
                Logging.Error($"[MANAGER ({GetType()})] Scanning error: {e}");

                InnerWrappersList.Clear();
                Status = AsyncScanManagerStatus.Error;
                ErrorMessage = e.Message;
            } finally {
                IsScanning = false;
                IsScanned = true;
            }
        }

        public override IAcManagerScanWrapper ScanWrapper {
            set { throw new NotSupportedException(); }
        }
    }
}