interface AvatarProps {
  size?: "xs" | "sm" | "md" | "lg";
  rounded?: "none" | "sm" | "md" | "lg" | "full";
}
const sizeClassMap: Record<NonNullable<AvatarProps["size"]>, string> = {
  xs: "w-8",
  sm: "w-10",
  md: "w-12",
  lg: "w-20",
};

const Avatar = ({ size = "sm", rounded = "md" }: AvatarProps) => {
  return (
    <div className="avatar">
      <div className={`${sizeClassMap[size]} rounded-${rounded}`}>
        <img src="https://img.daisyui.com/images/profile/demo/yellingcat@192.webp" />
      </div>
    </div>
  );
};

export default Avatar;
