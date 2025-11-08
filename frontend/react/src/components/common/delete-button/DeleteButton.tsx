import IconButton from "../icon-button/IconButton";
import type { DeleteButtonProps } from "./types";
import "./DeleteButton.scss";

const DeleteButton = ({ onClick }: DeleteButtonProps) => {
  const handleClick = () => {
    onClick?.();
  };

  return (
    <div className="delete-button">
      <IconButton iconName="delete" color="white" onClick={handleClick} />
    </div>
  );
};

export default DeleteButton;
