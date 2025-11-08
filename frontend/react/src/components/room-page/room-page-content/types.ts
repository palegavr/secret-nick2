import type {
  GetParticipantsResponse,
  GetRoomResponse,
  Participant,
} from "@types/api";

export interface RoomPageContentProps {
  participants: GetParticipantsResponse;
  roomDetails: GetRoomResponse;
  onDrawNames: () => void;
  onDeleteParticipant?(participant: Participant): void;
}
