import { useEffect, useState } from 'react';
// import { useNavigate } from 'react-router-dom';
import { useTheme } from '../../context/ThemeContext';
import MainLayout from '../components/MainLayout';

interface Task {
  id: string;
  title: string;
  description: string | null;
  scheduledFor: string;
  status: number;
}

type StatusEnum = 1 | 2 | 3;

const STATUS_OPTIONS = [
  { label: 'All Statuses', value: '' },
  { label: 'Active', value: 1 },
  { label: 'Deleted', value: 2 },
  { label: 'InActive', value: 3 },
];

const REMINDER_OPTIONS = [
  { label: 'At time', value: 0 },
  { label: '1 hour before', value: 1 },
  { label: '1 day before', value: 2 },
];

function formatStatus(status: number) {
  switch (status) {
    case 1: return 'Active';
    case 2: return 'Deleted';
    case 3: return 'InActive';
    default: return 'Unknown';
  }
}

export default function TasksPage() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<StatusEnum | ''>('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState({
    title: '',
    description: '',
    scheduledFor: '',
    reminderOffsets: [] as number[],
  });
  const [createError, setCreateError] = useState<string | null>(null);
  const [createLoading, setCreateLoading] = useState(false);

  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const { darkMode, toggleDarkMode } = useTheme();


  useEffect(() => {
        fetchTasks({
        search: search || undefined,
        status: status || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        pageOverride: page
    });
  }, [page]);

  const fetchTasks = async (filters: {
    search?: string;
    status?: StatusEnum | '';
    startDate?: string;
    endDate?: string;
    pageOverride?: number;
  }) => {
    setLoading(true);
    setError(null);
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      let utcStartDateString = "";
      if (filters.startDate) {
        utcStartDateString = new Date(filters.startDate).toISOString();
      }
      let utcEndDateString = "";
      if (filters.endDate) {
        const dateObj = new Date(filters.endDate);
        dateObj.setUTCHours(23, 59, 59, 999);
        utcEndDateString = dateObj.toISOString();
      }
      const params = new URLSearchParams({
        PageCount: '10',
        PageNumber: (filters.pageOverride ?? page).toString(),
        ...(filters.search ? { SearchString: filters.search } : {}),
        ...(filters.status ? { Status: filters.status.toString() } : {}),
        ...(utcStartDateString ? { UpcomingStartDate: utcStartDateString } : {}),
        ...(utcEndDateString ? { UpcomingEndDate: utcEndDateString } : {}),
      });
      const response = await fetch(`${baseUrl}/Tasks/users-tasks?${params.toString()}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'X-Api-Key': apiKey || '',
          'Content-Type': 'application/json',
        },
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to fetch tasks');
      setTasks(data.data.results);
      setTotalPages(data.data.totalPages);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch tasks');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    let effectiveEndDate = endDate;
    if (startDate && !endDate) {
      effectiveEndDate = new Date().toISOString().slice(0, 10);
      setEndDate(effectiveEndDate);
    }
    setPage(1);
    fetchTasks({
      search,
      status,
      startDate: startDate || undefined,
      endDate: (startDate ? (effectiveEndDate || new Date().toISOString().slice(0, 10)) : undefined),
      pageOverride: 1,
    });
  };

  const handleClearFilters = () => {
    setSearch('');
    setStatus('');
    setStartDate('');
    setEndDate('');
    setPage(1);
    fetchTasks({ pageOverride: 1 });
  };

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    setCreateLoading(true);
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const localDate = new Date(createForm.scheduledFor);
      const scheduledForUtc = localDate.toISOString();
      const response = await fetch(`${baseUrl}/Tasks/create`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'X-Api-Key': apiKey || '',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          title: createForm.title,
          description: createForm.description,
          scheduledFor: scheduledForUtc,
          reminderOffsets: createForm.reminderOffsets,
        }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to create task');
      setShowCreate(false);
      setCreateForm({ title: '', description: '', scheduledFor: '', reminderOffsets: [] });
      fetchTasks({});
    } catch (err: any) {
      setCreateError(err.message || 'Failed to create task');
    } finally {
      setCreateLoading(false);
    }
  };

  const handleDeleteTask = async (taskId: string) => {
    if (!window.confirm('Are you sure you want to delete this task?')) return;
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(`${baseUrl}/Tasks/delete`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'X-Api-Key': apiKey || '',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ taskId }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to delete task');
      fetchTasks({});
    } catch (err: any) {
      alert(err.message || 'Failed to delete task');
    }
  };

  const handleMarkDone = async (taskId: string) => {
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(`${baseUrl}/Tasks/mark-completed`, {
        method: 'PATCH',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'X-Api-Key': apiKey || '',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ taskId }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to mark task as done');
      fetchTasks({});
    } catch (err: any) {
      alert(err.message || 'Failed to mark task as done');
    }
  };

  const handleReminderChange = (offset: number) => {
    setCreateForm(f => {
      const exists = f.reminderOffsets.includes(offset);
      let next = exists
        ? f.reminderOffsets.filter(o => o !== offset)
        : [...f.reminderOffsets, offset];
      if (next.length > 3) next = next.slice(0, 3); // max 3
      return { ...f, reminderOffsets: next };
    });
  };

  const handleStartDateChange = (value: string) => {
    setStartDate(value);
    if (!endDate) {
      const today = new Date().toISOString().slice(0, 10);
      setEndDate(today);
    }
  };

  return (
    <MainLayout>
        <div className="w-full max-w-4xl mx-auto">
            <div className="flex justify-between items-center mb-8">
            <h1 className="text-3xl font-bold text-purple-700 dark:text-purple-300">Your Tasks</h1>
            <div className="flex items-center gap-2">
                <button onClick={toggleDarkMode} className="p-2 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700"
                aria-label="Toggle Dark Mode" >
                {darkMode ? (
                    <svg className="w-5 h-5 text-gray-900 dark:text-white" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path d="M21 12.79A9 9 0 1111.21 3a7 7 0 109.79 9.79z" />
                    </svg>
                ) : (
                    <svg className="w-5 h-5 text-gray-900 dark:text-white" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                    <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m16.364-6.364l-.707.707M6.343 17.657l-.707.707m12.728 0l-.707-.707M6.343 6.343l-.707-.707M12 7a5 5 0 000 10a5 5 0 000-10z" />
                    </svg>
                )}
                </button>
                <button className="bg-purple-700 text-white px-4 py-2 rounded hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600"
                onClick={() => setShowCreate(true)} >
                + New Task
                </button>
            </div>
            </div>

            <form onSubmit={handleSearch} className="flex flex-wrap gap-4 mb-6 items-end">
            <input type="text" placeholder="Search tasks..." className="p-2 rounded border bg-white dark:bg-gray-700"
                value={search} onChange={e => setSearch(e.target.value)} />
            <select className="p-2 rounded border bg-white dark:bg-gray-700" value={status}
                onChange={e => setStatus(e.target.value === '' ? '' : Number(e.target.value) as StatusEnum)} >
                {STATUS_OPTIONS.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
            </select>
            <div>
                <label className="block text-xs mb-1">Start Date</label>
                <input type="date" className="p-2 rounded border bg-white dark:bg-gray-700" value={startDate}
                onChange={e => handleStartDateChange(e.target.value)} max={undefined} />
            </div>
            <div>
                <label className="block text-xs mb-1">End Date</label>
                <input type="date" className="p-2 rounded border bg-white dark:bg-gray-700" value={endDate} onChange={e => setEndDate(e.target.value)}
                min={startDate || undefined} max={undefined} />
            </div>
            <button type="submit"
                className="bg-purple-700 text-white px-4 py-2 rounded hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600" >
                Filter
            </button>
            <button type="button"
                className="bg-gray-300 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-4 py-2 rounded hover:bg-gray-400 dark:hover:bg-gray-600"
                onClick={handleClearFilters} >
                Clear
            </button>
            </form>

            {showCreate && (
            <div className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50">
                <div className="bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg w-full max-w-md">
                <h2 className="text-xl font-bold mb-4 text-purple-700 dark:text-purple-300">Create Task</h2>
                <form onSubmit={handleCreateTask}>
                    <input type="text" placeholder="Title" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
                    value={createForm.title} onChange={e => setCreateForm(f => ({ ...f, title: e.target.value }))} required />
                    <textarea placeholder="Description" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
                    value={createForm.description} onChange={e => setCreateForm(f => ({ ...f, description: e.target.value }))} />
                    <input type="datetime-local" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
                    value={createForm.scheduledFor} onChange={e => setCreateForm(f => ({ ...f, scheduledFor: e.target.value }))}
                    required />
                    <div className="mb-4">
                    <div className="font-semibold mb-2">Reminders</div>
                    <div className="flex flex-col gap-2">
                        {REMINDER_OPTIONS.map(opt => (
                        <label key={opt.value} className="flex items-center gap-2">
                            <input type="checkbox" checked={createForm.reminderOffsets.includes(opt.value)}
                            onChange={() => handleReminderChange(opt.value)}
                            disabled={
                                !createForm.reminderOffsets.includes(opt.value) &&
                                createForm.reminderOffsets.length >= 3
                            } />
                            {opt.label}
                        </label>
                        ))}
                    </div>
                    <div className="text-xs text-gray-500 mt-1">
                        Select up to 3 reminders.
                    </div>
                    </div>
                    {createError && (
                    <div className="mb-4 p-2 bg-red-100 text-red-700 rounded">{createError}</div>
                    )}
                    <div className="flex justify-end gap-2">
                    <button type="button"
                        className="px-4 py-2 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                        onClick={() => setShowCreate(false)} >
                        Cancel
                    </button>
                    <button type="submit"
                        className="px-4 py-2 rounded bg-purple-700 text-white hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600"
                        disabled={createLoading}
                    >
                        {createLoading ? 'Creating...' : 'Create'}
                    </button>
                    </div>
                </form>
                </div>
            </div>
            )}

            {loading ? (
            <div className="text-center py-8">Loading tasks...</div>
            ) : error ? (
            <div className="text-center py-8 text-red-500">{error}</div>
            ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {tasks.map(task => (
                <div key={task.id} className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6 flex flex-col gap-2">
                    <div className="flex justify-between items-center">
                    <h3 className="font-bold text-lg text-purple-700 dark:text-purple-300">{task.title}</h3>
                    <span className={`px-2 py-1 rounded text-xs font-semibold ${
                        task.status === 1
                        ? 'bg-green-100 text-green-700'
                        : task.status === 2
                        ? 'bg-red-100 text-red-700'
                        : task.status === 3
                        ? 'bg-gray-200 text-gray-700'
                        : ''
                    }`}>
                        {formatStatus(task.status)}
                    </span>
                    </div>
                    <div className="text-gray-600 dark:text-gray-300 text-sm">{task.description || 'No description'}</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">
                    Scheduled: {new Date(task.scheduledFor).toLocaleString()}
                    </div>
                    <div className="flex gap-2 mt-2">
                    {task.status !== 2 && (
                        <>
                        {task.status !== 3 && (
                            <button className="px-3 py-1 rounded bg-green-600 text-white hover:bg-green-700 text-xs"
                            onClick={() => handleMarkDone(task.id)} >
                            Mark as Done
                            </button>
                        )}
                        <button className="px-3 py-1 rounded bg-red-600 text-white hover:bg-red-700 text-xs"
                            onClick={() => handleDeleteTask(task.id)} >
                            Delete
                        </button>
                        </>
                    )}
                    </div>
                </div>
                ))}
            </div>
            )}

            <div className="flex justify-center items-center gap-4 mt-8">
            <button className="px-3 py-1 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                onClick={() => {
                const newPage = Math.max(1, page - 1);
                setPage(newPage);
                fetchTasks({ pageOverride: newPage });
                }}
                disabled={page === 1} >
                Prev
            </button>
            <span>
                Page {page} of {totalPages}
            </span>
            <button className="px-3 py-1 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                onClick={() => {
                const newPage = Math.min(totalPages, page + 1);
                setPage(newPage);
                fetchTasks({ pageOverride: newPage });
                }}
                disabled={page === totalPages} >
                Next
            </button>
            </div>
        </div>
    </MainLayout>
  );
}