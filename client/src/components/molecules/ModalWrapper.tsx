type Props = {
  onClose: () => void;
  children: JSX.Element;
};

const ModalWrapper = ({ onClose, children }: Props) => {
  return (
    <div className="fixed left-0 bottom-18 z-50 flex justify-end items-end w-full h-full">
      <div
        className="bg-neutral opacity-80 w-full h-full absolute -z-40"
        onClick={() => onClose()}
      ></div>
      <div className="bg-base-300 p-6 flex w-full h-auto flex-1 flex-col shadow-lg rounded-t-2xl max-w-[66rem] mx-auto">
        {children}
      </div>
    </div>
  );
};

export default ModalWrapper;
