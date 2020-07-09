package main

import (
	"context"
	"errors"
	"fmt"
	"log"
	"net"
	"sync"

	pb "szcw/cz/Rime"

	uuid "github.com/google/uuid"
	grpc "google.golang.org/grpc"
)

var (
	zport    string = ":50010"
	roomName string = "szcw"
	seaworld string = "seaworld"
	exhibit  string = "exhibit"
	dragon   string = "dragon"
)

// Connection des
type Connection struct {
	stream pb.Exhibit_CreateStreamServer
	id     string
	active bool
	// 类型 0 = nreallight 1 = visit device
	deviceType string
	error      chan error
}

// Server : des
type Server struct {
	rooms map[string][]string

	players map[string]*pb.Player

	Connection []*Connection
}

func (s *Server) isExistPlayer(roomID string, playerID string) bool {
	playerids, _ := s.rooms[roomID]

	for _, v := range playerids {
		if v == playerID {
			return true
		}
	}
	return false
}

func (s *Server) isExistRoom(roomID string) bool {
	_, isExist := s.rooms[roomID]
	return isExist
}

//getPlayer : get player
func (s *Server) getPlayer(roomid string) []*pb.Player {
	var _players []*pb.Player
	for _, id := range s.rooms[roomid] {
		_players = append(_players, s.players[id])
	}
	return _players
}

// get index of romms map
func getIndexOfRoomArray(ps []string, p string) int {
	for i, _p := range ps {
		if _p == p {
			return i
		}
	}
	return -1
}

func unset(s []string, i int) []string {
	if i >= len(s) {
		return s
	}
	return append(s[:i], s[i+1:]...)
}

// Join : des
func (s *Server) Join(ctx context.Context, req *pb.JoinRequest) (*pb.JoinResponse, error) {

	roomID := req.GetRoomId()

	playerUUID, _ := uuid.NewRandom()
	playerID := playerUUID.String()

	if !s.isExistRoom(roomID) {
		s := fmt.Sprintf("[ZLOG] [ERROR] %s's room not found\n", roomID)
		return nil, errors.New(s)
	}

	s.rooms[roomID] = append(s.rooms[roomID], playerID)

	s.players[playerID] = &pb.Player{
		PlayerId: playerID,
		Position: &pb.ZPosition{},
		Rotation: &pb.ZRotation{},
	}

	log.Printf("[ZLOG] [INFO] Join Room => %s", playerID)

	return &pb.JoinResponse{
		PlayerId: playerID,
	}, nil
}

// SyncPose des
func (s *Server) SyncPose(ctx context.Context, req *pb.SyncRequest) (*pb.SyncResponse, error) {

	roomID := req.GetRoomId()
	playerID := req.GetPlayer().GetPlayerId()

	if !s.isExistRoom(roomID) {
		s := fmt.Sprintf("[ZLOG] [ERROR] %s's room not found\n", roomID)
		return nil, errors.New(s)
	}

	if !s.isExistPlayer(roomID, playerID) {
		s := fmt.Sprintf("[ZLOG] [ERROR] %s's player not found\n", playerID)
		return nil, errors.New(s)
	}

	// all players update pose
	position := req.GetPlayer().GetPosition()
	s.players[playerID].Position.X = position.GetX()
	s.players[playerID].Position.Y = position.GetY()
	s.players[playerID].Position.Z = position.GetZ()
	rotation := req.GetPlayer().GetRotation()
	s.players[playerID].Rotation.EulerX = rotation.GetEulerX()
	s.players[playerID].Rotation.EulerY = rotation.GetEulerY()
	s.players[playerID].Rotation.EulerZ = rotation.GetEulerZ()

	return &pb.SyncResponse{
		Players: s.getPlayer(roomID),
	}, nil
}

// Leave : reomve player
func (s *Server) Leave(ctx context.Context, req *pb.LeaveRequest) (*pb.LeaveResponse, error) {
	roomID := req.GetRoomId()
	playerID := req.GetPlayerId()

	if !s.isExistRoom(roomID) {

		s := fmt.Sprintf("[ZLOG] [ERROR] %s's room not found\n", roomID)
		return nil, errors.New(s)
	}

	if !s.isExistPlayer(roomID, playerID) {
		s := fmt.Sprintf("[ZLOG] [ERROR] %s's player not found\n", playerID)
		return nil, errors.New(s)
	}

	index := getIndexOfRoomArray(s.rooms[roomID], playerID)
	s.rooms[roomID] = unset(s.rooms[roomID], index)

	log.Println("[ZLOG] [INFO] Leave Room ---> ", playerID)

	return &pb.LeaveResponse{}, nil
}

// CreateStream : save client stream
func (s *Server) CreateStream(pbconn *pb.Connect, stream pb.Exhibit_CreateStreamServer) error {
	conn := &Connection{
		stream:     stream,
		id:         pbconn.Player.PlayerId,
		active:     true,
		deviceType: pbconn.DeviceType,
		error:      make(chan error),
	}

	s.Connection = append(s.Connection, conn)

	return <-conn.error
}

// BroadcastMessage : broadcast msg to client
func (s *Server) BroadcastMessage(ctx context.Context, msg *pb.Message) (*pb.Close, error) {
	wait := sync.WaitGroup{}
	done := make(chan int)

	for _, conn := range s.Connection {
		wait.Add(1)

		go func(msg *pb.Message, conn *Connection) {
			defer wait.Done()

			if conn.active {
				err := conn.stream.Send(msg)
				log.Printf("[ZLOG] [INFO] Sending message to => Id : %v - msg.content : %s", conn.id, msg.Content)

				if err != nil {
					log.Printf("[ZLOG] [ERROR] stream send msg => Stream: %v - Error: %v", conn.stream, err)
					conn.active = false
					conn.error <- err
				}
			}
		}(msg, conn)
	}

	go func() {
		wait.Wait()
		close(done)
	}()

	<-done
	return &pb.Close{}, nil
}

func main() {
	log.Println("[ZLOG] [INFO] Server run ...")

	var connections []*Connection

	server := &Server{
		rooms:      map[string][]string{roomName: {}},
		players:    map[string]*pb.Player{},
		Connection: connections,
	}

	grpcServer := grpc.NewServer()

	listener, err := net.Listen("tcp", zport)

	if err != nil {
		log.Fatal("[ZLOG] [ERROR] creating the server : ", err)
	}

	log.Println("[ZLOG] [INFO] Starting server at port ", zport)

	pb.RegisterExhibitServer(grpcServer, server)
	grpcServer.Serve(listener)

}
