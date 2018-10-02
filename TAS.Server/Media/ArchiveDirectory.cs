﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class ArchiveDirectory : MediaDirectoryBase, IArchiveDirectory
    {
        internal ArchiveDirectory(IMediaManager mediaManager, ulong id, string folder) 
        {
            IdArchive = id;
            Folder = folder;
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            return EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid);
        }

        internal void ArchiveSave(ServerMedia media, bool deleteAfterSuccess)
        {
            ArchiveMedia archived;
            if (media.IsArchived
                && (archived = EngineController.Database.ArchiveMediaFind<ArchiveMedia>(this, media.MediaGuid)) != null
                && archived.FileExists())
            {
                if (deleteAfterSuccess)
                {
                    MediaManager.FileManager.Queue(new FileOperation((FileManager)MediaManager.FileManager) { Kind = TFileOperationKind.Delete, Source = media },
                        false);
                }
            }
            else
                _archiveCopy(media, this, deleteAfterSuccess, false);
        }

        public void ArchiveRestore(IArchiveMedia srcMedia, IServerDirectory destDirectory, bool toTop)
        {
            _archiveCopy((MediaBase)srcMedia, destDirectory, false, toTop);
        }

        public ulong IdArchive { get; set; }

        public List<IArchiveMedia> Search(TMediaCategory? category, string searchString)
        {
            return EngineController.Database.ArchiveMediaSearch<ArchiveMedia>(this, category, searchString).ToList<IArchiveMedia>();
        }


        public void SweepStaleMedia()
        {
            IEnumerable<IMedia> staleMediaList = EngineController.Database.FindArchivedStaleMedia<ArchiveMedia>(this);
            foreach (var m in staleMediaList)
                m.Delete();
        }


        public override void RemoveMedia(IMedia media)
        {
            if (!(media is ArchiveMedia am))
                throw new ApplicationException("Media provided to RemoveMedia is not ArchiveMedia");
            am.MediaStatus = TMediaStatus.Deleted;
            am.IsVerified = false;
            am.Save();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            var newFileName = mediaProperties.FileName;
            if (File.Exists(Path.Combine(Folder, newFileName)))
            {
                Logger.Trace("{0}: File {1} already exists", nameof(CreateMedia), newFileName);
                newFileName = FileUtils.GetUniqueFileName(Folder, newFileName);
            }
            var result = new ArchiveMedia
            {
                MediaName = mediaProperties.MediaName,
                MediaGuid = mediaProperties.MediaGuid,
                LastUpdated = mediaProperties.LastUpdated,
                MediaType = mediaProperties.MediaType,
                Folder = GetCurrentFolder(),
                FileName = newFileName,
                MediaStatus = TMediaStatus.Required,
            };
            result.CloneMediaProperties(mediaProperties);
            return result;
        }

        internal string GetCurrentFolder()
        {
            return DateTime.UtcNow.ToString("yyyyMM");
        }

        private void _archiveCopy(MediaBase fromMedia, IMediaDirectory destDirectory, bool deleteAfterSuccess, bool toTop)
        {
            var operation = new FileOperation((FileManager)MediaManager.FileManager) { Kind = deleteAfterSuccess ? TFileOperationKind.Move : TFileOperationKind.Copy, Source = fromMedia, DestDirectory = destDirectory };
            operation.Success += _archived;
            operation.Failure += _failure;
            MediaManager.FileManager.Queue(operation, toTop);
        }

        private void _failure(object sender, EventArgs e)
        {
            if (!(sender is FileOperation operation))
                return;
            operation.Success -= _archived;
            operation.Failure -= _failure;
        }

        private void _archived(object sender, EventArgs e)
        {
            if (!(sender is FileOperation operation))
                return;
            if (operation.Source is ServerMedia sourceMedia)
                sourceMedia.IsArchived = true;
            operation.Success -= _archived;
            operation.Failure -= _failure;
        }
    }
}
