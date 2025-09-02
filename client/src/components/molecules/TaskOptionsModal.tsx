import React, { useState } from "react";
import ChevronDownIcon from "../../assets/icons/chevronDown.svg?react";
import PencilIcon from "../../assets/icons/pencil.svg?react";
import TrashIcon from "../../assets/icons/trash.svg?react";
import ModalWrapper from "./ModalWrapper";
import { TaskStatus } from "../../app/types/Task";

const STATUS_OPTIONS = [
  { value: TaskStatus.Pending, label: "Pending", color: "bg-yellow-500" },
  { value: TaskStatus.InProgress, label: "In Progress", color: "bg-blue-500" },
  { value: TaskStatus.Done, label: "Done", color: "bg-green-500" },
];

interface TaskOptionsModalProps {
  currentStatus: TaskStatus;
  onUpdateStatus: (newStatus: TaskStatus) => void | Promise<void>;
  onUpdate: () => void;
  onDelete: () => void;
  onClose: () => void;
}

const findStatusLabel = (value: TaskStatus) =>
  STATUS_OPTIONS.find((s) => s.value === value)?.label ?? value;

const TaskOptionsModal: React.FC<TaskOptionsModalProps> = ({
  currentStatus,
  onUpdateStatus,
  onUpdate,
  onDelete,
  onClose,
}) => {
  const [showStatusDropdown, setShowStatusDropdown] = useState(false);
  const [selectedStatus, setSelectedStatus] =
    useState<TaskStatus>(currentStatus);
  const [isUpdating, setIsUpdating] = useState(false);

  const [showConfirm, setShowConfirm] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<TaskStatus | null>(null);

  const currentStatusObj =
    STATUS_OPTIONS.find((s) => s.value === currentStatus) || STATUS_OPTIONS[0];

  const handleSelectOption = (option: TaskStatus) => {
    if (option === currentStatus) {
      setSelectedStatus(option);
      setShowStatusDropdown(false);
      return;
    }

    setPendingStatus(option);
    setShowConfirm(true);
  };

  const handleConfirmChange = async () => {
    if (!pendingStatus || pendingStatus === currentStatus) {
      setShowConfirm(false);
      setPendingStatus(null);
      return;
    }

    try {
      setIsUpdating(true);
      await Promise.resolve(onUpdateStatus(pendingStatus));
      setSelectedStatus(pendingStatus);
      setShowStatusDropdown(false);
      setShowConfirm(false);
      setPendingStatus(null);
    } catch (err) {
      console.error("Failed to update status", err);
    } finally {
      setIsUpdating(false);
    }
  };

  return (
    <>
      <ModalWrapper onClose={onClose}>
        <>
          <div className="flex flex-col flex-grow">
            <div className="flex justify-end items-center mb-2">
              <button
                onClick={onClose}
                aria-label="Close options"
                className="text-gray-600 hover:text-gray-900"
              >
                ✕
              </button>
            </div>

            <div className="mb-6">
              <div className="flex gap-2 items-center">
                <div
                  className={`w-3 h-3 rounded-full ${currentStatusObj.color}`}
                />
                <button
                  className="w-full font-medium text-left pl-4 flex justify-between items-center"
                  onClick={() => setShowStatusDropdown((s) => !s)}
                >
                  <span>Change Status</span>
                  <ChevronDownIcon
                    className={`w-4 h-4 transition-transform ${
                      showStatusDropdown ? "rotate-180" : ""
                    }`}
                  />
                </button>
              </div>

              {showStatusDropdown && (
                <div className="ml-8 mt-2 flex flex-col gap-2">
                  {STATUS_OPTIONS.map((option) => (
                    <button
                      key={option.value}
                      className={`flex items-center gap-2 p-2 rounded hover:bg-base-200 text-left ${
                        selectedStatus === option.value ? "bg-base-200" : ""
                      }`}
                      onClick={() =>
                        handleSelectOption(option.value as TaskStatus)
                      }
                    >
                      <div className={`w-2 h-2 rounded-full ${option.color}`} />
                      <span>{option.label}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="mb-12 flex gap-2">
              <PencilIcon className="w-5 h-5 mt-0.5" />
              <button
                className="w-full font-medium text-left pl-4"
                onClick={onUpdate}
              >
                Update
              </button>
            </div>

            <div className="mt-auto mb-6 flex gap-1 bg-red-400 py-2 px-1 rounded-lg w-full justify-center">
              <TrashIcon className="w-5 h-5 mt-0.5" />
              <button className="font-medium text-left" onClick={onDelete}>
                Delete
              </button>
            </div>
          </div>
        </>
      </ModalWrapper>

      {showConfirm && pendingStatus && (
        <ModalWrapper onClose={() => setShowConfirm(false)}>
          <>
            <div className="p-4">
              <h3 className="text-lg font-semibold mb-2">
                Confirm status change
              </h3>
              <p className="text-sm text-gray-600 mb-4">
                Are you sure you want to change the status from{" "}
                <strong>{findStatusLabel(currentStatus)}</strong> to{" "}
                <strong>{findStatusLabel(pendingStatus)}</strong>?
              </p>

              <div className="flex justify-end gap-2">
                <button
                  className="btn btn-ghost"
                  onClick={() => {
                    setShowConfirm(false);
                    setPendingStatus(null);
                  }}
                  disabled={isUpdating}
                >
                  Cancel
                </button>

                <button
                  className="btn btn-primary"
                  onClick={handleConfirmChange}
                  disabled={isUpdating}
                >
                  {isUpdating ? "Applying..." : "Confirm"}
                </button>
              </div>
            </div>
          </>
        </ModalWrapper>
      )}
    </>
  );
};

export default TaskOptionsModal;
