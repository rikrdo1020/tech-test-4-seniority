import { msalInstance } from "../services/msalConfig";
import { useState, useEffect, type ReactNode } from "react";
import {
  type AccountInfo,
  type AuthenticationResult,
} from "@azure/msal-browser";
import { AuthContext } from "../hooks/useAuth";

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<AccountInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const init = async () => {
      try {
        await msalInstance.initialize?.();
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
          setUser(accounts[0]);
        }
      } catch (err) {
        console.error("Error initializing MSAL:", err);
      } finally {
        setLoading(false);
      }
    };

    init();
  }, []);

  const login = async () => {
    try {
      const response: AuthenticationResult = await msalInstance.loginPopup({
        scopes: [import.meta.env.VITE_ENTRA_SCOPE],
      });
      setUser(response.account);
    } catch (err: unknown) {
      if (
        typeof err === "object" &&
        err !== null &&
        "errorCode" in err &&
        (err as { errorCode?: string }).errorCode === "user_cancelled"
      ) {
        console.warn("The user has canceled the login.");
      } else {
        console.error("Login failed:", err);
      }
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    msalInstance.logoutPopup();
    setUser(null);
  };

  if (loading) {
    return (
      <div className=" h-screen flex flex-col items-center justify-center gap-4">
        <img src="logo.png" className="h-30 w-26" alt="Loading..." />
        <p className="text-2xl font-bold text-primary">Loading...</p>
        <progress className="progress progress-primary w-56"></progress>
      </div>
    );
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
