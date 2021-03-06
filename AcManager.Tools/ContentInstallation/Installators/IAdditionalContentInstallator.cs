﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcManager.Tools.ContentInstallation.Entries;
using FirstFloor.ModernUI.Dialogs;
using JetBrains.Annotations;

namespace AcManager.Tools.ContentInstallation.Installators {
    public interface IAdditionalContentInstallator : IDisposable {
        string Password { get; }

        bool IsPasswordRequired { get; }

        bool IsNotSupported { get; }

        string NotSupportedMessage { get; }

        bool IsPasswordCorrect { get; }

        Task TrySetPasswordAsync(string password, CancellationToken cancellation);

        [ItemCanBeNull]
        Task<IReadOnlyList<ContentEntryBase>> GetEntriesAsync([CanBeNull] IProgress<AsyncProgressEntry> progress, CancellationToken cancellation);

        Task InstallAsync(ICopyCallback callback, [CanBeNull] IProgress<AsyncProgressEntry> progress, CancellationToken cancellation);
    }
}
