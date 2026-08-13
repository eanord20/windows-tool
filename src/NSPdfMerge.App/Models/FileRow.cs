using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NSPdfMerge.App.Models;

public sealed class FileRow : INotifyPropertyChanged
{
    private bool _include = true;
    private string _number = string.Empty;
    private string _title = string.Empty;
    private string _rowPath = string.Empty;
    private string _resolvedPath = string.Empty;
    private ResolveStatus _status = ResolveStatus.Pending;
    private List<string> _candidates = new();
    private int _candidatesCount;
    private bool _isDuplicate;
    private bool _isManuallyResolved;

    public bool Include
    {
        get => _include;
        set
        {
            if (value == _include) return;
            _include = value;
            OnPropertyChanged();
        }
    }

    public string Number
    {
        get => _number;
        set
        {
            if (value == _number) return;
            _number = value;
            OnPropertyChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public string RowPath
    {
        get => _rowPath;
        set
        {
            if (value == _rowPath) return;
            _rowPath = value;
            OnPropertyChanged();
        }
    }

    public string ResolvedPath
    {
        get => _resolvedPath;
        set
        {
            if (value == _resolvedPath) return;
            _resolvedPath = value;
            OnPropertyChanged();
        }
    }

    public List<string> Candidates
    {
        get => _candidates;
        set
        {
            if (ReferenceEquals(value, _candidates)) return;
            _candidates = value;
            OnPropertyChanged();

            CandidatesCount = _candidates.Count;
        }
    }

    public int CandidatesCount
    {
        get => _candidatesCount;
        private set
        {
            if (value == _candidatesCount) return;
            _candidatesCount = value;
            OnPropertyChanged();
        }
    }

    public bool IsDuplicate
    {
        get => _isDuplicate;
        set
        {
            if (value == _isDuplicate) return;
            _isDuplicate = value;
            OnPropertyChanged();
        }
    }

    public bool IsManuallyResolved
    {
        get => _isManuallyResolved;
        set
        {
            if (value == _isManuallyResolved) return;
            _isManuallyResolved = value;
            OnPropertyChanged();
        }
    }

    public ResolveStatus Status
    {
        get => _status;
        set
        {
            if (value == _status) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public FileRow Clone()
    {
        return new FileRow
        {
            Include = Include,
            Number = Number,
            Title = Title,
            RowPath = RowPath,
            ResolvedPath = ResolvedPath,
            Status = Status,
            Candidates = new List<string>(Candidates),
            IsDuplicate = IsDuplicate,
            IsManuallyResolved = IsManuallyResolved
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
