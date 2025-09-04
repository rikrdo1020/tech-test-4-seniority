import { Component, type ErrorInfo, type ReactNode } from "react";

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public state: ErrorBoundaryState = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Uncaught error:", error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }
      const handleRefresh = () => {
        window.location.reload();
      };
      return (
        <div className="p-4 text-center h-screen flex flex-col justify-center items-center">
          <div className="flex flex-col bg-red-100 text-red-800 justify-center p-6 rounded-md shadow-md max-w-lg">
            <h2 className="text-lg font-semibold">
              Oops! Something went wrong.
            </h2>
            <p className="text-sm mt-2">
              We're sorry for the inconvenience. Please try refreshing the page.
            </p>
            <button className="btn mt-4" onClick={handleRefresh}>
              {" "}
              Refresh
            </button>
            {import.meta.env.NODE_ENV === "development" && this.state.error && (
              <details className="mt-4 text-left text-xs text-red-700">
                <summary>Error Details</summary>
                <pre className="whitespace-pre-wrap break-all p-2 bg-red-50 rounded-sm mt-2">
                  {this.state.error.toString()}
                  <br />
                  {this.state.error.stack}
                </pre>
              </details>
            )}
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
