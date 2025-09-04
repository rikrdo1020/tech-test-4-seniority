import React from "react";
import { useNavigate } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useUpdateUser, useUser } from "../../app/hooks/useUsers";
import Input from "../atoms/Input";
import { useAuth } from "../../app/hooks/useAuth";

const schema = yup.object({
  name: yup.string().required("Name is required").min(1).max(100),
});

type FormValues = yup.Asserts<typeof schema>;

const UserEditFormPage: React.FC = () => {
  const { user, logout } = useAuth();
  const id = user?.localAccountId;
  const navigate = useNavigate();

  if (!id) {
    return <div className="p-6 text-red-600">User ID is missing.</div>;
  }

  const userQuery = useUser(id);
  const updateMutation = useUpdateUser(id);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<FormValues>({
    resolver: yupResolver(schema) as Resolver<FormValues>,
    defaultValues: {
      name: "",
    },
  });

  React.useEffect(() => {
    if (userQuery.data) {
      reset({
        name: userQuery.data.name ?? "",
      });
    }
  }, [userQuery.data, reset]);

  const onSubmit = async (values: FormValues) => {
    try {
      await updateMutation.mutateAsync(values);
      navigate(-1);
    } catch (err) {
      console.error("Update failed", err);
    }
  };

  if (userQuery.isLoading) return <div className="p-6">Loading user...</div>;
  if (userQuery.isError)
    return <div className="p-6 text-red-600">Failed to load user.</div>;

  return (
    <div className="max-w-xl mx-auto p-6 h-full flex flex-col flex-grow">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-semibold mb-4">My Profile</h1>
        <button className="btn btn-error lg:hidden" onClick={logout}>Logout</button>

      </div>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="space-y-4 flex flex-col flex-grow"
      >
        <div className="flex flex-col flex-grow gap-4">
          <Input
            label="Name"
            placeholder="Enter full name"
            {...register("name")}
            error={errors.name?.message}
          />

          {updateMutation.isError && (
            <div className="text-red-600">
              {updateMutation.error.message ?? "An error occurred."}
            </div>
          )}
        </div>

        <div className="flex gap-2 justify-end">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => reset()}
            disabled={isSubmitting || updateMutation.isPending}
          >
            Reset
          </button>

          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting || updateMutation.isPending}
          >
            {updateMutation.isPending ? "Saving..." : "Save Changes"}
          </button>
        </div>
      </form>

    </div>
  );
};

export default UserEditFormPage;
