import type { AccountInfo } from "@azure/msal-browser";
import { createContext, useContext } from "react";

interface AuthContextType {
  user: AccountInfo | null;
  login: () => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(
  undefined
);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
};
