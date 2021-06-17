import { getConnection } from '@/api/socket'
import websocket from '@/store/websocket'
import { Message } from 'element-ui'
import { serviceMsgNotice } from '@/utils/brower-notification'
// import { addNotice } from '@/utils/tag-notice'
import { getToken } from '@/utils/auth' // get token from cookie
import store from '@/store'

const state = { connecting: false }

export async function initSocket() {
  if (state.connecting) return
  state.connecting = true
  if (!websocket.getters.isClose) {
    state.connecting = false
    return
  }
  const token = getToken()
  if (!token) {
    state.connecting = false
    return
  }
  try {
    const conn = await getConnection({ negotiateVersion: 1, access_token: token })
    await websocket.dispatch('WEBSOCKET_INIT', `wss://${process.env.VUE_APP_Domain_Name}/chatHub?id=${conn.connectionToken}&access_token=${token}`)
  } catch (ex) {
    if (websocket.getters.isClose) {
      Message.error('socket 连接失败')
    }
  }
  state.connecting = false
}

export function closeSocket() {
  websocket.getters.close()
}
