import { useState, useEffect } from "react";
import MainLayout from "../components/MainLayout";
import { useNavigate } from 'react-router-dom';
import { useSession } from "../../context/SessionContext";
import { useTheme } from "../../context/ThemeContext";

interface User {
  userId: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  phoneNumber: string;
  status: string;
  roles: string[];
}

function getRoles(user: any): string[] {
  if (!user) return [];
  if (user.roles) return Array.isArray(user.roles) ? user.roles : [user.roles];
  if (user.Roles) return Array.isArray(user.Roles) ? user.Roles : [user.Roles];
  if (user["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"])
    return Array.isArray(user["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"])
      ? user["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      : [user["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]];
  return [];
}

export default function SettingsPage() {
  const { user, logout } = useSession();
  const { darkMode, toggleDarkMode } = useTheme();

  const [tab, setTab] = useState<"password" | "deactivate" | "users">("password");

  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [changePasswordLoading, setChangePasswordLoading] = useState(false);
  const [changePasswordError, setChangePasswordError] = useState<string | null>(null);
  const [changePasswordSuccess, setChangePasswordSuccess] = useState<string | null>(null);

  const [deactivateLoading, setDeactivateLoading] = useState(false);
  const [deactivateError, setDeactivateError] = useState<string | null>(null);
  const [showDeactivateConfirm, setShowDeactivateConfirm] = useState(false);

  const [users, setUsers] = useState<User[]>([]);
  const [usersLoading, setUsersLoading] = useState(false);
  const [usersError, setUsersError] = useState<string | null>(null);

  const [adminActionLoading, setAdminActionLoading] = useState<string | null>(null);
  const [adminActionError, setAdminActionError] = useState<string | null>(null);
  const [adminActionSuccess, setAdminActionSuccess] = useState<string | null>(null);

  // const [resetEmail, setResetEmail] = useState("");
  // const [resetLoading, setResetLoading] = useState(false);
  // const [resetError, setResetError] = useState<string | null>(null);
  // const [resetSuccess, setResetSuccess] = useState<string | null>(null);

  const roles = getRoles(user);
  const isAdmin = roles.includes("Admin");

  const [usersPage, setUsersPage] = useState(1);
  const USERS_PAGE_SIZE = 10;
  const [usersTotal, setUsersTotal] = useState(0);

  const navigate = useNavigate();

  function mapStatus(status: number | string): string {
  if (typeof status === "number") {
    switch (status) {
      case 0: return "Pending";
      case 1: return "Active";
      case 2: return "Deleted";
      case 3: return "Inactive";
      default: return String(status);
    }
  }

  if (status === "0") return "Pending";
  if (status === "1") return "Active";
  if (status === "2") return "Deleted";
  if (status === "3") return "Inactive";
  return status;
}

  useEffect(() => {
    if (!isAdmin) return;
    setUsersLoading(true);
    setUsersError(null);
    fetch(`${import.meta.env.VITE_API_BASE_URL}/account/admin/users?pageNumber=${usersPage}&pageCount=${USERS_PAGE_SIZE}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
        "X-Api-Key": import.meta.env.VITE_BASE_API_KEY,
      },
    })
      .then(async (res) => {
        const data = await res.json();
        if (!res.ok) throw new Error(data.message || "Failed to fetch users");
        setUsers(data.data.results);
        setUsersTotal(data.data.totalCount || 0);
      })
      .catch((err) => setUsersError(err.message))
      .finally(() => setUsersLoading(false));
  }, [isAdmin, adminActionSuccess, usersPage]);

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setChangePasswordError(null);
    setChangePasswordSuccess(null);
    setChangePasswordLoading(true);
    try {
      const res = await fetch(`${import.meta.env.VITE_API_BASE_URL}/account/change-password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Api-Key": import.meta.env.VITE_BASE_API_KEY,
          Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
        },
        body: JSON.stringify({
          newPassword,
          confirmNewPassword,
        }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to change password");
      setChangePasswordSuccess("Password changed successfully.");
      setNewPassword("");
      setConfirmNewPassword("");
    } catch (err: any) {
      setChangePasswordError(err.message);
    } finally {
      setChangePasswordLoading(false);
    }
  };

  const handleDeactivateAccount = async () => {
    setDeactivateError(null);
    setDeactivateLoading(true);
    try {
      const res = await fetch(`${import.meta.env.VITE_API_BASE_URL}/account/deactivate-account`, {
        method: "DELETE",
        headers: {
          "X-Api-Key": import.meta.env.VITE_BASE_API_KEY,
          Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
        },
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Failed to deactivate account");
      logout();
    } catch (err: any) {
      setDeactivateError(err.message);
    } finally {
      setDeactivateLoading(false);
      setShowDeactivateConfirm(false);
    }
  };

  const handleAdminAction = async (
    action: "activate" | "deactivate" | "delete" | "role" | "reset",
    userId: string,
    extra?: any
  ) => {
    setAdminActionError(null);
    setAdminActionSuccess(null);
    setAdminActionLoading(userId + action);
    let url = "";
    let method: "POST" | "DELETE" = "POST";
    let body: any = {};
    switch (action) {
      case "activate":
        url = `${import.meta.env.VITE_API_BASE_URL}/account/admin/activate-account`;
        body = { userId };
        break;
      case "deactivate":
        url = `${import.meta.env.VITE_API_BASE_URL}/account/admin/deactivate-account`;
        method = "DELETE";
        body = { userId };
        break;
      case "delete":
        url = `${import.meta.env.VITE_API_BASE_URL}/account/admin/delete-account`;
        method = "DELETE";
        body = { userId, isPermanant: true };
        break;
      case "role":
        url = `${import.meta.env.VITE_API_BASE_URL}/account/admin/change-role`;
        body = { userId, role: extra };
        break;
      case "reset":
        url = `${import.meta.env.VITE_API_BASE_URL}/account/password-reset/initial`;
        body = { emailAddress: extra };
        break;
      default:
        return;
    }
    try {
      const res = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
          "X-Api-Key": import.meta.env.VITE_BASE_API_KEY,
          Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
        },
        body: JSON.stringify(body),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.message || "Action failed");
      setAdminActionSuccess("Action successful.");
    } catch (err: any) {
      setAdminActionError(err.message);
    } finally {
      setAdminActionLoading(null);
    }
  };

  return (
    <MainLayout>
      <div className="w-full max-w-3xl mx-auto">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-bold text-purple-700 dark:text-purple-300">Settings</h1>
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
        </div>

        <div className="flex gap-2 mb-8 border-b">
          <button
            className={`px-4 py-2 -mb-px border-b-2 ${tab === "password" ? "border-purple-700 text-purple-700 dark:text-purple-300 font-bold" : "border-transparent"}`}
            onClick={() => setTab("password")} >
            Password
          </button>
          <button
            className={`px-4 py-2 -mb-px border-b-2 ${tab === "deactivate" ? "border-purple-700 text-purple-700 dark:text-purple-300 font-bold" : "border-transparent"}`}
            onClick={() => setTab("deactivate")} >
            Deactivate Account
          </button>
          {isAdmin && (
            <button
              className={`px-4 py-2 -mb-px border-b-2 ${tab === "users" ? "border-purple-700 text-purple-700 dark:text-purple-300 font-bold" : "border-transparent"}`}
              onClick={() => setTab("users")} >
              Users
            </button>
          )}
        </div>

        {tab === "password" && (
          <section className="mb-8">
            <form onSubmit={handleChangePassword} className="flex flex-col gap-4 max-w-md">
              <input type="password" placeholder="New Password" className="p-2 rounded border bg-white dark:bg-gray-700"
                value={newPassword} onChange={e => setNewPassword(e.target.value)} required />
              <input type="password" placeholder="Confirm New Password" className="p-2 rounded border bg-white dark:bg-gray-700"
                value={confirmNewPassword} onChange={e => setConfirmNewPassword(e.target.value)} required />
              {changePasswordError && <div className="text-red-500">{changePasswordError}</div>}
              {changePasswordSuccess && <div className="text-green-600">{changePasswordSuccess}</div>}
              <button type="submit"
                className="bg-purple-700 text-white px-4 py-2 rounded hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600"
                disabled={changePasswordLoading} >
                {changePasswordLoading ? "Changing..." : "Change Password"}
              </button>
            </form>
          </section>
        )}

        {tab === "deactivate" && (
          <section className="mb-8">
            <div className="flex flex-col gap-4 max-w-md">
              <div className="bg-yellow-50 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-100 p-4 rounded">
                <p>
                  Are you sure you want to deactivate your account? <br />
                  <b>Only support can reactivate your account.</b>
                </p>
              </div>
              <button className="bg-yellow-600 text-white px-4 py-2 rounded hover:bg-yellow-700" onClick={() => setShowDeactivateConfirm(true)}
                disabled={deactivateLoading} >
                {deactivateLoading ? "Deactivating..." : "Deactivate My Account"}
              </button>
              {deactivateError && <div className="text-red-500">{deactivateError}</div>}
            </div>

            {showDeactivateConfirm && (
              <div className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50">
                <div className="bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg w-full max-w-md">
                  <h2 className="text-lg font-bold mb-4 text-purple-700 dark:text-purple-300">Confirm Deactivation</h2>
                  <p className="mb-4">
                    Are you sure you want to deactivate your account? <br />
                    <b>Only support can reactivate your account.</b>
                  </p>
                  <div className="flex justify-end gap-2">
                    <button className="px-4 py-2 rounded bg-gray-300 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                      onClick={() => setShowDeactivateConfirm(false)} disabled={deactivateLoading} >
                      Cancel
                    </button>
                    <button className="px-4 py-2 rounded bg-yellow-600 text-white hover:bg-yellow-700" onClick={handleDeactivateAccount}
                      disabled={deactivateLoading} >
                      {deactivateLoading ? "Deactivating..." : "Yes, Deactivate"}
                    </button>
                  </div>
                </div>
              </div>
            )}
          </section>
        )}

        {tab === "users" && isAdmin && (
          <section className="mb-8">
            <h2 className="text-xl font-semibold mb-4 dark:text-white">Admin: Users</h2>
            <div className="mb-6">
              <h3 className="font-semibold mb-2">All Users</h3>
              {usersLoading ? (
                <div>Loading users...</div>
              ) : usersError ? (
                <div className="text-red-500">{usersError}</div>
              ) : (
                <div>
                  <div className="overflow-x-auto">
                    <table className="min-w-full text-sm">
                      <thead>
                        <tr>
                          <th className="text-left p-2">Name</th>
                          <th className="text-left p-2">Email</th>
                          <th className="text-left p-2">Status</th>
                          <th className="text-left p-2">Roles</th>
                          <th className="text-left p-2">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {users.map(u => (
                          <tr key={u.userId} className="border-t">
                            <td className="p-2">{u.firstName} {u.lastName}</td>
                            <td className="p-2">{u.emailAddress}</td>
                            <td className="p-2">{mapStatus(u.status)}</td>
                            <td className="p-2">{u.roles.join(", ")}</td>
                            <td className="p-2 flex flex-wrap gap-1">
                              <button className="px-2 py-1 rounded bg-green-600 text-white text-xs"
                                disabled={adminActionLoading === u.userId + "activate"}
                                onClick={() => handleAdminAction("activate", u.userId)} >
                                Activate
                              </button>
                              <button className="px-2 py-1 rounded bg-yellow-600 text-white text-xs"
                                disabled={adminActionLoading === u.userId + "deactivate"}
                                onClick={() => handleAdminAction("deactivate", u.userId)} >
                                Deactivate
                              </button>
                              <button className="px-2 py-1 rounded bg-red-600 text-white text-xs"
                                disabled={adminActionLoading === u.userId + "delete"}
                                onClick={() => handleAdminAction("delete", u.userId)} >
                                Delete
                              </button>
                              <button className="px-2 py-1 rounded bg-purple-700 text-white text-xs"
                                disabled={adminActionLoading === u.userId + "reset"}
                                onClick={() => handleAdminAction("reset", u.userId, u.emailAddress)} >
                                Reset Password
                              </button>
                              <select className="px-2 py-1 rounded border text-xs" defaultValue=""
                                onChange={e => {
                                  if (e.target.value)
                                    handleAdminAction("role", u.userId, e.target.value);
                                }} >
                                <option value="">Change Role</option>
                                <option value="Admin">Admin</option>
                                <option value="User">User</option>
                              </select>
                              <button className="px-2 py-1 rounded bg-blue-600 text-white text-xs" 
                                onClick={() => navigate(`/admin/user-tasks/${u.userId}`)} >
                                    View Tasks
                                </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className="flex justify-between items-center mt-4">
                    <button className="px-3 py-1 rounded bg-gray-200 dark:bg-gray-700" onClick={() => setUsersPage(p => Math.max(1, p - 1))}
                      disabled={usersPage === 1} >
                      Previous
                    </button>
                    <span>
                      Page {usersPage} of {Math.ceil(usersTotal / USERS_PAGE_SIZE) || 1}
                    </span>
                    <button className="px-3 py-1 rounded bg-gray-200 dark:bg-gray-700" onClick={() => setUsersPage(p => p + 1)}
                      disabled={usersPage >= Math.ceil(usersTotal / USERS_PAGE_SIZE)} >
                      Next
                    </button>
                  </div>
                  {adminActionError && <div className="text-red-500 mt-2">{adminActionError}</div>}
                  {adminActionSuccess && <div className="text-green-600 mt-2">{adminActionSuccess}</div>}
                </div>
              )}
            </div>
          </section>
        )}
      </div>
    </MainLayout>
  );
}