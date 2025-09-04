import { useAuth } from "../../app/hooks/useAuth";

export default function LoginPage() {
  const { login } = useAuth();

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gradient-to-br from-base-300 to-indigo-950 p-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-xl overflow-hidden">
        <div className="bg-primary p-6 text-center">
          <img src="logo.png" className="h-16 w-16 mx-auto text-white" />
          <h1 className="text-2xl font-bold text-white mt-2">MyTasks</h1>
        </div>

        <div className="p-8 text-center">
          <div className="space-y-6">
            <div className="space-y-3">
              <h2 className="text-2xl font-semibold text-gray-800">
                Welcome
              </h2>
              <p className="text-gray-600">
                Sign in to manage your tasks and collaborate with your team
              </p>
            </div>

            <div className="pt-4">
              <button
                onClick={login}
                className="w-full btn btn-primary"
              >
                <span className="flex items-center gap-2">
                  Continue
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
