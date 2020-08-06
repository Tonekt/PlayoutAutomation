﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.IngestDirectories
{
    public class IngestDirectoriesViewModel: OkCancelViewModelBase
    {
        private readonly string _fileName;
        private IngestDirectoryViewModel _selectedDirectory;

        private IEnumerable<IngestDirectory> _ingestDirectories;

        public IngestDirectoriesViewModel(string fileName)
        {
            _ingestDirectories = Deserialize(fileName);
            foreach (var item in _ingestDirectories.Select(d => new IngestDirectoryViewModel(d, this)))
            {
                Directories.Add(item);
            }
            _fileName = fileName;
            _createCommands();
        }

        public ICommand CommandAdd { get; private set; }

        public ICommand CommandDelete { get; private set; }

        public ICommand CommandUp { get; private set; }

        public ICommand CommandDown { get; private set; }

        public ICommand CommandAddSub { get; private set; }

        public ObservableCollection<IngestDirectoryViewModel> Directories { get; } = new ObservableCollection<IngestDirectoryViewModel>();
        
        public IngestDirectoryViewModel SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (_selectedDirectory == value)
                    return;
                _selectedDirectory = value;
                NotifyPropertyChanged();
                InvalidateRequerySuggested();
            }
        }
        
        public override bool IsModified { get { return base.IsModified || Directories.Any(d => d.IsModified); } }

        public override bool Ok(object obj = null)
        {
            Directories.ToList().ForEach(d => d.SaveToModel());
            var writer = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_fileName))
            {
                writer.Serialize(file, Directories.Select(d => d.IngestDirectory).ToList());
            }

            return true;
        }        

        private void _createCommands()
        {
            CommandAdd = new UiCommand(_add);
            CommandAddSub = new UiCommand(_addSub, _canAddSub);
            CommandDelete = new UiCommand(_delete, _canDelete);
            CommandUp = new UiCommand(_up, _canUp);
            CommandDown = new UiCommand(_down, _canDown);
        }

        private bool _canAddSub(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _addSub(object obj)
        {
            SelectedDirectory = SelectedDirectory.AddSubdirectory();
        }

        private void _delete(object obj)
        {
            if (!_deleteDirectory(_selectedDirectory))
                return;
            IsModified = true;
            SelectedDirectory = Directories.FirstOrDefault();
        }

        private bool _deleteDirectory(IngestDirectoryViewModel item)
        {
            var collection = item.OwnerCollection;
            if (!collection.Contains(item))
                return false;
            collection.Remove(item);
            return true;
        }

        private bool _canDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _add(object obj)
        {
            var newDir = new IngestDirectoryViewModel(new IngestDirectory(), this) { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            Directories.Add(newDir);
            IsModified = true;
            SelectedDirectory = newDir;
        }

        private void _up(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex > 0)
                {
                    collection.Move(oldIndex, oldIndex - 1);
                    IsModified = true;
                }
            }
        }

        private bool _canDown(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int index = collection.IndexOf(_selectedDirectory);
                return index >= 0 && index < collection.Count - 1;
            }
            return false;
        }

        private void _down(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex < collection.Count - 1)
                {
                    collection.Move(oldIndex, oldIndex + 1);
                    IsModified = true;
                }
            }
        }

        private bool _canUp(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            return collection?.IndexOf(_selectedDirectory) > 0;
        }

        private static IEnumerable<IngestDirectory> Deserialize(string fileName)
        {
            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                try
                {
                    return (IEnumerable<IngestDirectory>)reader.Deserialize(file);
                }
                finally
                {
                    file.Close();
                }
            }
            catch (NullReferenceException)
            {
                return new List<IngestDirectory>();
            }
        }       
    }
}
