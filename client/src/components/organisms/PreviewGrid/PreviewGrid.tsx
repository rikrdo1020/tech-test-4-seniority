import type { Counts } from "../../../app/types/Task";

type PreviewGridProps = Partial<Counts>;

const PreviewGrid = (counts: PreviewGridProps) => {
  return (
    <div className="grid grid-cols-2 grid-rows-2 gap-4">
      <div className="bg-linear-65 from-yellow-300 to-yellow-600 rounded-xl min-h-24">
        <div className="w-full h-full flex flex-col text-center justify-center">
          <div className="text-4xl font-bold">{counts.inProgress}</div>
          <p>In Progress</p>
        </div>
      </div>
      <div className="bg-linear-145 from-red-300 to-red-500 col-start-1 row-start-2 rounded-xl min-h-24">
        <div className="w-full h-full flex flex-col text-center justify-center">
          <div className="text-4xl font-bold">{counts.pending}</div>
          <p>Pending</p>
        </div>
      </div>
      <div className="bg-linear-145 from-green-300 to-green-600 row-span-2 col-start-2 row-start-1 rounded-xl min-h-24">
        <div className="w-full h-full flex flex-col text-center justify-center">
          <div className="text-4xl font-bold">{counts.done}</div>
          <p>Done</p>
        </div>
      </div>
    </div>
  );
};

export default PreviewGrid;
