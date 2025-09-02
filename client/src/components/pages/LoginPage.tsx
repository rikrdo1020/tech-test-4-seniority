import { useAuth } from "../../app/hooks/useAuth";

export default function LoginPage() {
  const { user, login, logout } = useAuth();

  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      {user ? (
        <>
          <p>Welcome, {user.name}</p>
          <button onClick={logout} className="btn">
            Logout
          </button>
        </>
      ) : (
        <button onClick={login} className="btn">
          Login with Microsoft
        </button>
      )}
    </div>
  );
}
