import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import MainLayout from "../components/MainLayout";
import { useTheme } from "../../context/ThemeContext";

interface Task {
  id: string;
  title: string;
  description: string | null;
  scheduledFor: string;
  status: number;
}

const STATUS_OPTIONS = [
  { label: "All Statuses", value: "" },
  { label: "Active", value: 1 },
  { label: "Deleted", value: 2 },
  { label: "InActive", value: 3 },
];

export default function AdminUserTasksPage() {
  const { userId } = useParams();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<number | "">("");
  const [search, setSearch] = useState("");
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const { darkMode, toggleDarkMode } = useTheme();

  useEffect(() => {
    fetchTasks();
  }, [userId, page, status, search, startDate, endDate]);

  const fetchTasks = async (opts?: { pageOverride?: number }) => {
    setLoading(true);
    setError(null);
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      let utcStartDateString = "";
      if (startDate) {
        utcStartDateString = new Date(startDate).toISOString();
      }
      let utcEndDateString = "";
      if (endDate) {
        const dateObj = new Date(endDate);
        dateObj.setUTCHours(23, 59, 59, 999);
        utcEndDateString = dateObj.toISOString();
      }
      const params = new URLSearchParams({
        PageCount: "10",
        PageNumber: (opts?.pageOverride ?? page).toString(),
      ...(userId ? { UserId: userId } : {}),
      ...(status ? { Status: status.toString() } : {}),
      ...(search ? { SearchString: search } : {}),
      ...(utcStartDateString ? { UpcomingStartDate: utcStartDateString } : {}),
      ...(utcEndDateString ? { UpcomingEndDate: utcEndDateString } : {}),
      });
      const response = await fetch(`${baseUrl}/Tasks/admin/all-tasks?${params.toString()}`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
          "X-Api-Key": apiKey || "",
          "Content-Type": "application/json",
        },
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || "Failed to fetch tasks");
      setTasks(data.data.results);
      setTotalPages(data.data.totalPages);
    } catch (err: any) {
      setError(err.message || "Failed to fetch tasks");
    } finally {
      setLoading(false);
    }
  };

  const handleFilter = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    fetchTasks({ pageOverride: 1 });
  };

  return (
    <MainLayout>
      <div className="w-full max-w-4xl mx-auto">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-bold text-purple-700 dark:text-purple-300">User's Tasks</h1>
          <button onClick={toggleDarkMode} className="p-2 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700" aria-label="Toggle Dark Mode" >
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
        </div>
        <form onSubmit={handleFilter} className="flex gap-4 mb-6 flex-wrap items-end">
            <input type="text" placeholder="Search tasks..." className="p-2 rounded border bg-white dark:bg-gray-700" value={search}
                onChange={e => setSearch(e.target.value)} />
            <select className="p-2 rounded border bg-white dark:bg-gray-700" value={status}
                onChange={e => setStatus(e.target.value === "" ? "" : Number(e.target.value))} >
                {STATUS_OPTIONS.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
            </select>
            <div>
                <label className="block text-xs mb-1">Start Date</label>
                <input type="date" className="p-2 rounded border bg-white dark:bg-gray-700" value={startDate}
                onChange={e => setStartDate(e.target.value)} max={undefined} />
            </div>
            <div>
                <label className="block text-xs mb-1">End Date</label>
                <input type="date" className="p-2 rounded border bg-white dark:bg-gray-700" value={endDate}
                onChange={e => setEndDate(e.target.value)} min={startDate || undefined} max={undefined}
                />
            </div>
            <button type="submit"
                className="bg-purple-700 text-white px-4 py-2 rounded hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600" >
                Filter
            </button>
        </form>
        {loading ? (
          <div>Loading...</div>
        ) : error ? (
          <div className="text-red-500">{error}</div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {tasks.map(task => (
              <div key={task.id} className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6 flex flex-col gap-2">
                <h3 className="font-bold text-lg text-purple-700 dark:text-purple-300">{task.title}</h3>
                <div className="text-gray-600 dark:text-gray-300 text-sm">{task.description || "No description"}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400">
                  Scheduled: {new Date(task.scheduledFor).toLocaleString()}
                </div>
                <span className={`px-2 py-1 rounded text-xs font-semibold ${
                  task.status === 1
                    ? "bg-green-100 text-green-700"
                    : task.status === 2
                    ? "bg-red-100 text-red-700"
                    : task.status === 3
                    ? "bg-gray-200 text-gray-700"
                    : ""
                }`}>
                  {["", "Active", "Deleted", "InActive"][task.status]}
                </span>
              </div>
            ))}
          </div>
        )}
        <div className="flex justify-center items-center gap-4 mt-8">
          <button className="px-3 py-1 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
            onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} >
            Prev
          </button>
          <span>
            Page {page} of {totalPages}
          </span>
          <button className="px-3 py-1 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
            onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} >
            Next
          </button>
        </div>
      </div>
    </MainLayout>
  );
}