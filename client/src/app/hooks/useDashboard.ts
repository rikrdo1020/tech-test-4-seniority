import { useQuery } from "@tanstack/react-query";
import type { DashboardData } from "../types/Task";
import { apiFetch } from "../services/apiClient";

export const useDashboard = () => {
  return useQuery<DashboardData>({
    queryKey: ["dashboard"],
    queryFn: ({ signal }) => apiFetch("/dashboard", { method: "GET" }, signal),
  });
};
