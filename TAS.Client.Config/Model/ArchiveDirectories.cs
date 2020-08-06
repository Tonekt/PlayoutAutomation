﻿using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class ArchiveDirectories
    {
        public List<ArchiveDirectory> Directories { get; }
        private readonly IDatabase _db;

        public ArchiveDirectories(IDatabase db)
        {
            _db = db;
            Directories = db.LoadArchiveDirectories<ArchiveDirectory>().ToList();
            Directories.ForEach(d => d.IsModified = false);
        }

        public void Save()
        {
            foreach (var dir in Directories.ToList())
            {
                if (dir.IsDeleted)
                {
                    _db.DeleteArchiveDirectory(dir);
                    Directories.Remove(dir);
                }
                else
                if (dir.IsNew)
                    _db.InsertArchiveDirectory(dir);
                else
                if (dir.IsModified)
                    _db.UpdateArchiveDirectory(dir);
            }
        }
    }
}
