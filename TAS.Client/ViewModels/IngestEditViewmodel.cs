﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    class IngestEditViewmodel : ViewmodelBase
    {
        private readonly ObservableCollection<ConvertOperationViewModel> _conversionList;
        public IngestEditViewmodel(IList<IConvertOperation> convertionList)
        {
            _conversionList = new ObservableCollection<ConvertOperationViewModel>(from op in convertionList select new ConvertOperationViewModel(op));
            SelectedOperation = _conversionList.FirstOrDefault();
            foreach (var c in _conversionList)
                c.PropertyChanged += new PropertyChangedEventHandler(_convertOperationPropertyChanged);
        }

        void _convertOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsValid")
                NotifyPropertyChanged(e.PropertyName);
        }

        public ObservableCollection<ConvertOperationViewModel> OperationList { get { return _conversionList; } }

        private ConvertOperationViewModel _selectedOperation;
        public ConvertOperationViewModel SelectedOperation
        {
            get { return _selectedOperation; }
            set { SetField(ref _selectedOperation, value, "SelectedOperation"); }
        }

        public bool ShowMediaList
        {
            get { return _conversionList.Count > 1; }
        }

        protected override void OnDispose()
        {
            foreach (var c in _conversionList)
            {
                c.PropertyChanged -= new PropertyChangedEventHandler(_convertOperationPropertyChanged);
                c.Dispose();
            }
        }

        
        public bool IsValid
        {
            get
            {
                foreach (ConvertOperationViewModel mediaVm in _conversionList)
                {
                    if (!mediaVm.IsValid)
                        return false;
                    if (_conversionList.Count(c => c.DestFileName == mediaVm.DestFileName) > 1)
                        return false;
                }
                return true;
            }
        }


    
    }
}
